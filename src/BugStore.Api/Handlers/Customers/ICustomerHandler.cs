using BugStore.Api.Requests.Customers;

namespace BugStore.Api.Handlers.Customers;

public interface ICustomerHandler
{
    Task<IResult> GetAllAsync();
    Task<IResult> GetByIdAsync(Guid id);
    Task<IResult> CreateAsync(Create request); 
    Task<IResult> UpdateAsync(Guid id, Update request);
    Task<IResult> DeleteAsync(Guid id);
}