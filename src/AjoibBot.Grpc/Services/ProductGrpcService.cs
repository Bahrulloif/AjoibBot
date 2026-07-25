using AjoibBot.Application.Interfaces;
using Grpc.Core;

namespace AjoibBot.Grpc.Services;

public class ProductGrpcService : ProductService.ProductServiceBase
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductGrpcService> _logger;

    public ProductGrpcService(
        IProductRepository repository,
        ILogger<ProductGrpcService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // Реализация GetAll
    public override async Task<ProductList> GetAll(
        GetAllRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetAll вызван");

        var products = await _repository.GetAllAsync(context.CancellationToken);

        // Маппинг Entity → Protobuf message
        var response = new ProductList();
        foreach (var product in products)
        {
            response.Products.Add(new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Price = (double)product.Price,
                Size = product.Size ?? "",
                Color = product.Color ?? "",
                StockQuantity = product.StockQuantity,
                CategoryName = product.Category?.Name ?? ""
            });
        }

        return response;
    }

    // Реализация GetById
    public override async Task<ProductResponse> GetById(
        GetByIdRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetById вызван: id={Id}", request.Id);

        var product = await _repository.GetByIdAsync(request.Id, context.CancellationToken);

        if (product is null)
        {
            // В gRPC ошибки передаются через RpcException
            throw new RpcException(new Status(
                StatusCode.NotFound,
                $"Товар с id={request.Id} не найден"));
        }

        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Price = (double)product.Price,
            Size = product.Size ?? "",
            Color = product.Color ?? "",
            StockQuantity = product.StockQuantity,
            CategoryName = product.Category?.Name ?? ""
        };
    }
}