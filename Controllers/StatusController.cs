using Microsoft.AspNetCore.Mvc;
using WrapPasswordAssessment.Models;

namespace WrapPasswordAssessment.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StatusController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApplicationStatus> Get()
    {
        return Ok(new ApplicationStatus(
            "Wrap Password Assessment",
            "Available",
            DateTimeOffset.UtcNow));
    }
}
