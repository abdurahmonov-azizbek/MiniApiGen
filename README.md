# 🔧 Minimal API Generator for .NET

Hi there! 👋

This is a **small and fun side project** I built in my free time — it automatically generates RESTful APIs in ASP.NET Core Minimal APIs just by adding **one attribute** to your entity.

It’s super lightweight and uses reflection, generics, and attributes to register the following endpoints automatically:

- `POST /{entity}/create`
- `GET /{entity}/get-all`
- `GET /{entity}/get/{id}`
- `PUT /{entity}/update`
- `DELETE /{entity}/delete/{id}`
- `DELETE /{entity}/delete`
- `GET /{entity}/query?search=...&orderBy=asc&page=1&pageSize=20`

---

## ✨ Features

✔️ Minimal setup – just one attribute  
✔️ Built-in pagination, ordering, and search  
✔️ No boilerplate controllers  
✔️ Ideal for prototyping, testing, or learning  

---

## 🚀 How to Use

1. Mark your entity with `[ApiEntity]`  
2. Optionally add `[Searchable]` to string properties  
3. Call `app.RegisterApis();` in your `Program.cs`

```csharp
[ApiEntity]
public class Product : IEntity<int>
{
    public int Id { get; set; }

    [Searchable]
    public string Name { get; set; }

    public decimal Price { get; set; }
}

