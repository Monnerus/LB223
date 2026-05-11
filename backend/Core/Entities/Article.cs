namespace Core.Entities;

public class Article
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
