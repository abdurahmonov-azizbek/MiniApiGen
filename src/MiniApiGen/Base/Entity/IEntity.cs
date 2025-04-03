namespace MiniApiGen.Base.Entity;

public interface IEntity<TKey>
{
    TKey Id { get; set; }
}