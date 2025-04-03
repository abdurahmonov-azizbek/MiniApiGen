using MiniApiGen.Attributes;
using MiniApiGen.Base.Entity;

namespace MiniApiGen.Entities;

[ApiEntity]
public class Book : IEntity<Guid>
{
    public Guid Id { get; set; }
    
    [Searchable]
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    
    [Searchable]
    public string Author { get; set; } = null!;
    public string Publisher { get; set; } = null!;
    public DateTime PublishedDate { get; set; }
}