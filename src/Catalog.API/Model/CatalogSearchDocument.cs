namespace eShop.Catalog.API.Model;

public record CatalogSearchDocument(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int CatalogBrandId,
    int CatalogTypeId,
    string? PictureFileName,
    int AvailableStock);
