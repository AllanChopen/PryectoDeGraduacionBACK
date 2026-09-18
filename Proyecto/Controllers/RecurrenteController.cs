using Microsoft.AspNetCore.Mvc;
using Proyecto.Models.Recurrente;
using Proyecto.Services.Recurrente;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecurrenteController : ControllerBase
{
    private readonly IRecurrenteService _recurrenteService;

    public RecurrenteController(IRecurrenteService recurrenteService)
    {
        _recurrenteService = recurrenteService;
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var response = await _recurrenteService.TestConnectionAsync();

            return Ok(new
            {
                success = true,
                recurrenteResponse = response
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpPost("connected-account")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateConnectedAccount(
        [FromForm] CreateConnectedAccountRequest request)
    {
        try
        {
            var response = await _recurrenteService.CreateConnectedAccountAsync(
                request.Email,
                request.FullName,
                request.PhoneNumber,
                request.Name,
                request.TaxRegistrationDocument,
                request.IdCardImage,
                request.IdCardImageBack
            );

            return Ok(new
            {
                success = true,
                recurrenteResponse = response
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }
}
