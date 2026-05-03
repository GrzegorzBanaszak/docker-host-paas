using Dockerizer.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Dockerizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DnsController(IJobsService jobsService, IDnsService dnsService) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var overview = await dnsService.GetOverviewAsync(cancellationToken);
        return Ok(overview);
    }

    [HttpGet("routes")]
    public async Task<IActionResult> GetRoutes(CancellationToken cancellationToken)
    {
        var routes = await dnsService.GetRoutesAsync(cancellationToken);
        return Ok(routes);
    }

    [HttpGet("routes/{jobId:guid}")]
    public async Task<IActionResult> GetRoute(Guid jobId, CancellationToken cancellationToken)
    {
        var route = await dnsService.GetRouteAsync(jobId, cancellationToken);
        return route is null ? NotFound() : Ok(route);
    }

    [HttpPost("routes/{jobId:guid}/publish")]
    public async Task<IActionResult> PublishRoute(Guid jobId, CancellationToken cancellationToken)
    {
        try
        {
            var job = await jobsService.EnablePublicRouteAsync(jobId, cancellationToken);
            return job is null ? NotFound() : Ok(job);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(jobId), ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpDelete("routes/{jobId:guid}/publish")]
    public async Task<IActionResult> UnpublishRoute(Guid jobId, CancellationToken cancellationToken)
    {
        try
        {
            var job = await jobsService.DisablePublicRouteAsync(jobId, cancellationToken);
            return job is null ? NotFound() : Ok(job);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(jobId), ex.Message);
            return ValidationProblem(ModelState);
        }
    }
}
