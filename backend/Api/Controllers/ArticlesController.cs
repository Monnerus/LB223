using Api.DTOs;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ArticlesController(IArticleRepository repo) : ControllerBase
{
    // GET api/articles
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var articles = await repo.GetAllAsync();
        return Ok(articles.Select(a => new ArticleDto(a.Id, a.Name, a.Quantity)));
    }

    // POST api/articles/{id}/book
    [HttpPost("{id}/book")]
    public async Task<IActionResult> Book(int id, [FromBody] BookingRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest("Menge muss grösser als 0 sein.");

        try
        {
            await repo.BookAsync(id, request.Type, request.Amount);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}
