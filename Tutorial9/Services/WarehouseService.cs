using System.Data;
using Microsoft.Data.SqlClient;
using Tutorial9.Exceptions;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class WarehouseService : IWarehouseService
{
    private readonly string _connectionString = "Data Source=localhost, 1433; User=SA; Password=QAZqaz123; Integrated Security=False;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False";


    public async Task<int> AddRequest(RequestToAdd requestToAdd)
    {
        await using SqlConnection con = new SqlConnection(_connectionString);
        await using SqlCommand cmd = new SqlCommand();
        cmd.Connection = con;
        await con.OpenAsync();
        
        //Sprawdzenie obecności produktu
        cmd.CommandText = @"SELECT COUNT(*) FROM Product WHERE IdProduct = @ProductId";
        cmd.Parameters.AddWithValue("@ProductId", requestToAdd.IdProduct);
        int productCheck = (int)await cmd.ExecuteScalarAsync();
        if ( productCheck == 0 )
        {
            throw new NotFoundException($"Produkt o podanym ID ({requestToAdd.IdProduct}) nie istnieje.");
        }
        
        //Sprawdzenie obecności magazynu
        cmd.Parameters.Clear();
        cmd.CommandText = @"SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @WarehouseId";
        cmd.Parameters.AddWithValue("@WarehouseId", requestToAdd.IdWarehouse);
        int warehouseCheck = (int)await cmd.ExecuteScalarAsync();
        if ( warehouseCheck == 0 )
        {
            throw new NotFoundException($"Magazyn o podanym ID ({requestToAdd.IdWarehouse}) nie istnieje.");
        }

        //Sprawdzenie podanej ilości
        if (requestToAdd.Amount <= 0)
            throw new ConflictException($"Podana ilość produktów ({requestToAdd.Amount}) jest niepoprawna.");

        //Sprawdzanie parametrów zamówienia
        cmd.Parameters.Clear();
        cmd.CommandText = @"SELECT IdOrder FROM [Order] WHERE IdProduct = @ProductId 
                                 AND Amount = @ProductAmount 
                                 AND CreatedAt < @ProductCreatedAt";
        cmd.Parameters.AddWithValue("@ProductId", requestToAdd.IdProduct);
        cmd.Parameters.AddWithValue("@ProductAmount", requestToAdd.Amount);
        cmd.Parameters.AddWithValue("@ProductCreatedAt", requestToAdd.CreatedAt);
        var orderId = await cmd.ExecuteScalarAsync();
        if ( orderId == null )
        {
            throw new NotFoundException($"Zamówienie z podanymi wartościami nie istnieje.");
        }
        
        //Sprawdzenie już zrealizowanego zamówienia
        cmd.Parameters.Clear();
        cmd.CommandText = @"SELECT COUNT(*) FROM Product_Warehouse WHERE IdOrder = @OrderId";
        cmd.Parameters.AddWithValue("@OrderId", orderId);
        int orderCheck = (int)await cmd.ExecuteScalarAsync();
        if ( orderCheck != 0 )
        {
            throw new ConflictException($"Zamówienie już zostało zrealizowane.");
        }
        
        //Aktualizacja zamówienia
        cmd.Parameters.Clear();
        cmd.CommandText = @"UPDATE [Order] SET FulfilledAt = GETDATE() WHERE IdOrder = @OrderId";
        cmd.Parameters.AddWithValue("@OrderId", orderId);
        int rowsAffected = await cmd.ExecuteNonQueryAsync();
        if ( rowsAffected == 0 )
        {
            throw new ConflictException($"Błąd aktualizacji zamówienia.");
        }
        
        //Wstawienie zrealizowanego zamówienia do magazynu
        cmd.Parameters.Clear();
        cmd.CommandText = @"INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                            VALUES(@WarehouseId, @ProductId, @OrderId, @ProductAmount, 
                                   (SELECT Price FROM Product WHERE IdProduct = @ProductId) * @ProductAmount, 
                                   @ProductCreatedAt)
                            SELECT SCOPE_IDENTITY();";
        cmd.Parameters.AddWithValue("@WarehouseId", requestToAdd.IdWarehouse);
        cmd.Parameters.AddWithValue("@ProductId", requestToAdd.IdProduct);
        cmd.Parameters.AddWithValue("@OrderId", orderId);
        cmd.Parameters.AddWithValue("@ProductAmount", requestToAdd.Amount);
        cmd.Parameters.AddWithValue("@ProductCreatedAt", requestToAdd.CreatedAt);
        int result = await cmd.ExecuteNonQueryAsync();
        if ( result == 0 )
        {
            throw new Exception($"Błąd podczas realizacji zamówienia.");
        }
        
        return Convert.ToInt32(result);
    }

    public async Task AddRequestProcedure(RequestToAdd requestData)
    {
        await using SqlConnection con = new SqlConnection(_connectionString);
        await using SqlCommand com = new SqlCommand();
        
        com.Connection = con;
        await con.OpenAsync();
        
        com.CommandText = "AddProductToWarehouse";
        com.CommandType = CommandType.StoredProcedure;
        com.Parameters.AddWithValue("@IdProduct", requestData.IdProduct);
        com.Parameters.AddWithValue("@Amount", requestData.Amount);
        com.Parameters.AddWithValue("@CreatedAt", requestData.CreatedAt);
        com.Parameters.AddWithValue("@IdWarehouse", requestData.IdWarehouse);
        
        await com.ExecuteNonQueryAsync();
        
    }
}