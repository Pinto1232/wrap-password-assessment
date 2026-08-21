using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WrapPasswordAssessment.Data;
using WrapPasswordAssessment.Models;

namespace WrapPasswordAssessment.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StatusController : ControllerBase
{
    private readonly ApplicationDbContext _database;

    public StatusController(ApplicationDbContext database)
    {
        _database = database;
    }

    [HttpGet]
    public async Task<ActionResult<ApplicationStatus>> Get(CancellationToken cancellationToken)
    {
        var metadata = await _database.ApplicationMetadata
            .AsNoTracking()
            .SingleAsync(cancellationToken);

        return Ok(new ApplicationStatus(
            metadata.Name,
            "Available",
            "SQLite",
            DateTimeOffset.UtcNow));
    }
}
