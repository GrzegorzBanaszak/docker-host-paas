using Dockerizer.Api.Contracts.Projects;
using Dockerizer.Application.Abstractions;
using Dockerizer.Application.Projects;
using Microsoft.AspNetCore.Mvc;

namespace Dockerizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProjectsController(IProjectsService projectsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var projects = await projectsService.GetAllAsync(cancellationToken);
        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var project = await projectsService.GetByIdAsync(id, cancellationToken);
        return project is null ? NotFound() : Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectApiRequest request, CancellationToken cancellationToken)
    {
        var validationProblem = ValidateProjectRequest(request.Name, request.RepositoryUrl);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        var project = await projectsService.CreateAsync(
            new CreateProjectCommand(request.Name, request.RepositoryUrl, request.DefaultBranch, request.DefaultProjectPath),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectApiRequest request, CancellationToken cancellationToken)
    {
        var validationProblem = ValidateProjectRequest(request.Name, request.RepositoryUrl);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        var project = await projectsService.UpdateAsync(
            id,
            new UpdateProjectCommand(request.Name, request.RepositoryUrl, request.DefaultBranch, request.DefaultProjectPath),
            cancellationToken);

        return project is null ? NotFound() : Ok(project);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var archived = await projectsService.ArchiveAsync(id, cancellationToken);
        return archived ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/jobs")]
    public async Task<IActionResult> CreateJob(Guid id, [FromBody] CreateProjectJobApiRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var project = await projectsService.CreateJobAsync(
                id,
                new CreateProjectJobCommand(request.Name, request.Branch, request.ProjectPath),
                cancellationToken);

            return project is null ? NotFound() : Ok(project);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(id), ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpGet("{id:guid}/jobs")]
    public async Task<IActionResult> GetJobs(Guid id, CancellationToken cancellationToken)
    {
        var project = await projectsService.GetByIdAsync(id, cancellationToken);
        return project is null ? NotFound() : Ok(project.Jobs);
    }

    [HttpGet("{id:guid}/images")]
    public async Task<IActionResult> GetImages(Guid id, CancellationToken cancellationToken)
    {
        var project = await projectsService.GetByIdAsync(id, cancellationToken);
        return project is null ? NotFound() : Ok(project.Images);
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> PublishRoute(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var project = await projectsService.EnablePublicRouteAsync(id, cancellationToken);
            return project is null ? NotFound() : Ok(project);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(id), ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpDelete("{id:guid}/publish")]
    public async Task<IActionResult> UnpublishRoute(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var project = await projectsService.DisablePublicRouteAsync(id, cancellationToken);
            return project is null ? NotFound() : Ok(project);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(id), ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    private IActionResult? ValidateProjectRequest(string name, string repositoryUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(name), "Name is required.");
        }

        if (string.IsNullOrWhiteSpace(repositoryUrl))
        {
            ModelState.AddModelError(nameof(repositoryUrl), "RepositoryUrl is required.");
        }
        else if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out _))
        {
            ModelState.AddModelError(nameof(repositoryUrl), "RepositoryUrl must be a valid absolute URI.");
        }

        return ModelState.IsValid ? null : ValidationProblem(ModelState);
    }
}
