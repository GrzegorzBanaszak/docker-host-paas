using Dockerizer.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Dockerizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SystemController(ISystemResourceService systemResourceService) : ControllerBase
{
    [HttpGet("resources")]
    public async Task<IActionResult> GetResources(CancellationToken cancellationToken)
    {
        var snapshot = await systemResourceService.GetSnapshotAsync(cancellationToken);
        return Ok(snapshot);
    }
}
