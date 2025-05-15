using Microsoft.AspNetCore.Mvc;
using Tutorial9.Exceptions;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers;


[Route("api/[controller]")]
[ApiController]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehouseController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }
    
    [HttpPost]
    public async Task<IActionResult> AddRequest([FromBody] RequestToAdd requestData)
    {
        try
        {
            var result = await _warehouseService.AddRequest(requestData);
            return Ok(result);
        }
        catch (NotFoundException nfe)
        {
            return NotFound(nfe.Message);
        }
        catch (ConflictException ce)
        {
            return Conflict(ce.Message);
        }
        catch (Exception e)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPost("procedure")]
    public async Task<IActionResult> AddRequestProcedure([FromBody] RequestToAdd requestData)
    {
        try
        {
            await _warehouseService.AddRequestProcedure(requestData);
            return Ok(1);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }
}