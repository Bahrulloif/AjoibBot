namespace AjoibBot.Application.DTOs;

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Size { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
}