using Dockerizer.Api.Contracts.Jobs;
using Dockerizer.Application.Abstractions;
using Dockerizer.Application.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace Dockerizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class JobsController(IJobsService jobsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var jobs = await jobsService.GetAllAsync(cancellationToken);
        return Ok(jobs);
    }

    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches([FromQuery] string repositoryUrl, [FromQuery] string? projectPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(repositoryUrl))
        {
            ModelState.AddModelError(nameof(repositoryUrl), "RepositoryUrl is required.");
            return ValidationProblem(ModelState);
        }

        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out _))
        {
            ModelState.AddModelError(nameof(repositoryUrl), "RepositoryUrl must be a valid absolute URI.");
            return ValidationProblem(ModelState);
        }

        try
        {
            var inspection = await jobsService.GetRepositoryInspectionAsync(repositoryUrl, projectPath, cancellationToken);
            return Ok(inspection);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(repositoryUrl), ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var job = await jobsService.GetByIdAsync(id, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpGet("{id:guid}/logs")]
    public async Task<IActionResult> GetLogs(Guid id, CancellationToken cancellationToken)
    {
        var job = await jobsService.GetByIdAsync(id, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        var logs = await jobsService.GetLogsAsync(id, cancellationToken);
        return Ok(logs ?? new JobLogDto(string.Empty));
    }

    [HttpGet("{id:guid}/files")]
    public async Task<IActionResult> GetFiles(Guid id, CancellationToken cancellationToken)
    {
        var files = await jobsService.GetFilesAsync(id, cancellationToken);
        return Ok(files);
    }

    [HttpGet("{id:guid}/files/{fileId}")]
    public async Task<IActionResult> GetFileContent(Guid id, string fileId, CancellationToken cancellationToken)
    {
        var file = await jobsService.GetFileContentAsync(id, fileId, cancellationToken);
        return file is null ? NotFound() : Ok(file);
    }

    [HttpPost("{id:guid}/container/start")]
    public async Task<IActionResult> StartContainer(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var job = await jobsService.StartContainerAsync(id, cancellationToken);
            return job is null ? NotFound() : Ok(job);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(id), ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpPost("{id:guid}/container/restart")]
    public async Task<IActionResult> RestartContainer(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var job = await jobsService.RestartContainerAsync(id, cancellationToken);
            return job is null ? NotFound() : Ok(job);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(id), ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpPost("{id:guid}/container/stop")]
    public async Task<IActionResult> StopContainer(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var job = await jobsService.StopContainerAsync(id, cancellationToken);
            return job is null ? NotFound() : Ok(job);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(id), ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var job = await jobsService.RetryAsync(id, cancellationToken);
            return job is null ? NotFound() : Ok(job);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(id), ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpPost("{id:guid}/rebuild")]
    public async Task<IActionResult> Rebuild(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var job = await jobsService.RebuildAsync(id, cancellationToken);
            return job is null ? NotFound() : Ok(job);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(id), ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var job = await jobsService.CancelAsync(id, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await jobsService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobApiRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Name is required.");
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            ModelState.AddModelError(nameof(request.RepositoryUrl), "RepositoryUrl is required.");
            return ValidationProblem(ModelState);
        }

        if (!Uri.TryCreate(request.RepositoryUrl, UriKind.Absolute, out _))
        {
            ModelState.AddModelError(nameof(request.RepositoryUrl), "RepositoryUrl must be a valid absolute URI.");
            return ValidationProblem(ModelState);
        }

        var createdJob = await jobsService.CreateAsync(
            new CreateJobCommand(request.Name, request.RepositoryUrl, request.Branch, request.ProjectPath, request.ProjectId),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = createdJob.Id }, createdJob);
    }
}
