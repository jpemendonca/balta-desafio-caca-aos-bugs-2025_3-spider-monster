using BugStore.Api.Requests.Products;

namespace BugStore.Api.Handlers.Product;

public interface IProductHandle
{
    Task<IResult> GetAllAsync();
    Task<IResult> GetByIdAsync(Guid id);
    Task<IResult> CreateAsync(Create request); 
    Task<IResult> UpdateAsync(Guid id, Update request);
    Task<IResult> DeleteAsync(Guid id);
}