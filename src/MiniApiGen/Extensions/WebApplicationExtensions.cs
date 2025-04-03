using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApiGen.AppDbContexts;
using MiniApiGen.Attributes;
using MiniApiGen.Base.Entity;
using MiniApiGen.Filter;

namespace MiniApiGen.Extensions;

public static class EndpointExtensions
{
    public static WebApplication RegisterApis(this WebApplication app)
    {
        app.UseEndpoints(endpoints =>
        {
            var entityTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type => type.GetCustomAttribute<ApiEntityAttribute>() != null);

            foreach (var entityType in entityTypes)
            {
                var name = entityType.Name.ToLower();

                var postMethod = typeof(EndpointExtensions).GetMethod(nameof(HandleCreate),
                    BindingFlags.Static | BindingFlags.NonPublic);
                var genericPostMethod = postMethod!.MakeGenericMethod(entityType);
                genericPostMethod.Invoke(null, [app, name]);

                var getAllMethod = typeof(EndpointExtensions).GetMethod(nameof(HandleGetAll),
                    BindingFlags.Static | BindingFlags.NonPublic);
                var genericGetAllMethod = getAllMethod!.MakeGenericMethod(entityType);
                genericGetAllMethod.Invoke(null, [app, name]);

                var idType = GetIdType(entityType);
                if (idType is not null)
                {
                    var getByIdMethod = typeof(EndpointExtensions).GetMethod(nameof(HandleGetById),
                        BindingFlags.Static | BindingFlags.NonPublic);
                    var genericGetByIdMethod = getByIdMethod!.MakeGenericMethod(entityType, idType);
                    genericGetByIdMethod.Invoke(null, [app, name]);

                    var updateMethod = typeof(EndpointExtensions).GetMethod(nameof(HandleUpdate),
                        BindingFlags.Static | BindingFlags.NonPublic);
                    var genericUpdateMethod = updateMethod!.MakeGenericMethod(entityType, idType);
                    genericUpdateMethod.Invoke(null, [app, name]);

                    var deleteByIdMethod = typeof(EndpointExtensions).GetMethod(nameof(HandleDeleteById),
                        BindingFlags.Static | BindingFlags.NonPublic);
                    var genericDeleteByIdMethod = deleteByIdMethod!.MakeGenericMethod(entityType, idType);
                    genericDeleteByIdMethod.Invoke(null, [app, name]);

                    var deleteMethod = typeof(EndpointExtensions).GetMethod(nameof(HandleDelete),
                        BindingFlags.Static | BindingFlags.NonPublic);
                    var genericDeleteMethod = deleteMethod!.MakeGenericMethod(entityType, idType);
                    genericDeleteMethod.Invoke(null, [app, name]);

                    var queryMethod = typeof(EndpointExtensions).GetMethod(nameof(HandleQuery),
                        BindingFlags.Static | BindingFlags.NonPublic);
                    var genericQueryMethod = queryMethod!.MakeGenericMethod(entityType, idType);
                    genericQueryMethod.Invoke(null, [app, name]);
                }
            }
        });

        return app;
    }

    private static void HandleGetAll<T>(WebApplication app, string route) where T : class
    {
        app.MapGet($"/{route}/get-all", async ([FromServices] AppDbContext context) =>
        {
            var result = await context.Set<T>().ToListAsync();
            return Results.Ok(result);
        });
    }

    private static void HandleCreate<T>(WebApplication app, string route) where T : class
    {
        app.MapPost($"/{route}/create", async ([FromServices] AppDbContext context, [FromBody] T entity) =>
        {
            await context.Set<T>().AddAsync(entity);
            await context.SaveChangesAsync();
            return Results.Created($"/{route}/{entity}", entity);
        });
    }

    private static void HandleGetById<T, TKey>(WebApplication app, string route)
        where T : class, IEntity<TKey>
    {
        app.MapGet($"/{route}/get/{{id}}", async ([FromServices] AppDbContext context, [FromRoute] TKey id) =>
        {
            var entity = await context.Set<T>().FindAsync(id);
            return entity is not null ? Results.Ok(entity) : Results.NotFound();
        });
    }

    private static Type GetIdType(Type entityType)
    {
        return entityType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>))
            ?.GetGenericArguments().First()!;
    }

    private static void HandleUpdate<T, TKey>(WebApplication app, string route) where T : class, IEntity<TKey>
    {
        app.MapPut($"/{route}/update", async ([FromServices] AppDbContext context, [FromBody] T entity) =>
        {
            var existEntity = context.Set<T>().AsNoTracking().FirstOrDefault(x => x.Id!.Equals(entity.Id));
            if (existEntity is null)
                return Results.NotFound();

            context.Set<T>().Update(entity);
            await context.SaveChangesAsync();
            return Results.Ok(entity);
        });
    }

    private static void HandleDeleteById<T, TKey>(WebApplication app, string route)
        where T : class, IEntity<TKey>
    {
        app.MapDelete($"/{route}/delete/{{id}}", async ([FromServices] AppDbContext context, [FromRoute] TKey id) =>
        {
            var entity = await context.Set<T>().FindAsync(id);
            if (entity is null)
                return Results.NotFound();

            context.Set<T>().Remove(entity);
            await context.SaveChangesAsync();
            return Results.Ok();
        });
    }

    private static void HandleDelete<T, TKey>(WebApplication app, string route)
        where T : class, IEntity<TKey>
    {
        app.MapDelete($"/{route}/delete", async ([FromServices] AppDbContext context, [FromBody] T entity) =>
        {
            var existEntity = await context.Set<T>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id!.Equals(entity.Id));
                
            if (existEntity is null)
                return Results.NotFound();

            context.Set<T>().Remove(entity);
            await context.SaveChangesAsync();
            return Results.Ok();
        });
    }

    private static void HandleQuery<T, TKey>(WebApplication app, string route) 
        where T : class, IEntity<TKey>
    {
        app.MapGet($"/{route}/query",
            async ([FromServices] AppDbContext context, 
                [FromQuery] string? search,
                [FromQuery] string orderBy = "asc",
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                var query = context.Set<T>().AsQueryable();

                if(!string.IsNullOrWhiteSpace(search))
                {
                    query = ApplySearch(query, search, typeof(T));
                }

                query = ApplyOrdering<T, TKey>(query, orderBy);
                query = ApplyPagination(query, page, pageSize);

                return Results.Ok(new PagedResult<T>
                {
                    Data = query.ToList(),
                    TotalCount = query.Count(),
                    Page = page,
                    PageSize = pageSize,
                });
            });
    }

    private static IQueryable<T> ApplyOrdering<T, TKey>(IQueryable<T> query, string orderBy) where T : class, IEntity<TKey>
    {
        if(orderBy == "asc")
            query = query.OrderBy(x => x.Id);
        else if(orderBy == "desc")
            query = query.OrderByDescending(x => x.Id);
    
        return query;
    }       

    private static IQueryable<T> ApplyPagination<T>(IQueryable<T> query, int page, int pageSize) where T : class
    {
        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }

    private static IQueryable<T> ApplySearch<T>(IQueryable<T> query, string search, Type entityType)
    {
        var searchableProps = typeof(T).GetProperties()
            .Where(p => p.GetCustomAttribute(typeof(SearchableAttribute)) is not null).ToList();

        if (!searchableProps.Any())
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        var searchExpressions = new List<Expression>();

        foreach (var prop in searchableProps)
        {
            if (prop.PropertyType != typeof(string))
                continue;

            var propertyAccess = Expression.Property(parameter, prop);
            var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });
            var constant = Expression.Constant(search, typeof(string));
            var containsCall = Expression.Call(propertyAccess, containsMethod!, constant);
            searchExpressions.Add(containsCall);
        }

        if (!searchExpressions.Any())
        {
            return query;
        }

        var orExpression = searchExpressions.Aggregate(Expression.OrElse);
        var lambda = Expression.Lambda<Func<T, bool>>(orExpression, parameter);

        return query.Where(lambda);
    }
}

