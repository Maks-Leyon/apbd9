using Tutorial9.Model;

namespace Tutorial9.Services;

public interface IWarehouseService
{
    public Task<int> AddRequest(RequestToAdd requestToAdd);
    public Task AddRequestProcedure(RequestToAdd requestData);
}