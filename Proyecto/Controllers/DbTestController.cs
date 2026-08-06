using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DbTestController : ControllerBase
{
    private readonly AppDbContext _context;

    public DbTestController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DbTest>>> GetAll()
    {
        var rows = await _context.DbTests.AsNoTracking().ToListAsync();
        return Ok(rows);
    }

    [HttpGet("{prueba}")]
    public async Task<ActionResult<DbTest>> GetByPrueba(string prueba)
    {
        var row = await _context.DbTests.AsNoTracking().FirstOrDefaultAsync(x => x.Prueba == prueba);
        if (row is null)
        {
            return NotFound();
        }

        return Ok(row);
    }

    [HttpPost]
    public async Task<ActionResult<DbTest>> Create([FromBody] DbTest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prueba))
        {
            return BadRequest("Prueba es obligatorio.");
        }

        await _context.Database.ExecuteSqlInterpolatedAsync($"insert into dbtest (prueba) values ({request.Prueba})");
        return CreatedAtAction(nameof(GetByPrueba), new { prueba = request.Prueba }, request);
    }

    [HttpPut("{prueba}")]
    public async Task<IActionResult> Update(string prueba, [FromBody] DbTest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prueba))
        {
            return BadRequest("Prueba es obligatorio.");
        }

        var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"update dbtest set prueba = {request.Prueba} where prueba = {prueba}");
        if (affected == 0)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{prueba}")]
    public async Task<IActionResult> Delete(string prueba)
    {
        var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"delete from dbtest where prueba = {prueba}");
        if (affected == 0)
        {
            return NotFound();
        }

        return NoContent();
    }
}
