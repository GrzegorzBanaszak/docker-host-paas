using Dockerizer.Application.Abstractions;
using Dockerizer.Application.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace Dockerizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ImagesController(IImagesService imagesService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var images = await imagesService.GetAllAsync(cancellationToken);
        return Ok(images);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var image = await imagesService.GetByIdAsync(id, cancellationToken);
        return image is null ? NotFound() : Ok(image);
    }

    [HttpGet("{id:guid}/logs")]
    public async Task<IActionResult> GetLogs(Guid id, CancellationToken cancellationToken)
    {
        var logs = await imagesService.GetLogsAsync(id, cancellationToken);
        return Ok(logs ?? new JobLogDto(string.Empty));
    }

    [HttpGet("{id:guid}/files")]
    public async Task<IActionResult> GetFiles(Guid id, CancellationToken cancellationToken)
    {
        var files = await imagesService.GetFilesAsync(id, cancellationToken);
        return Ok(files);
    }

    [HttpGet("{id:guid}/files/{fileId}")]
    public async Task<IActionResult> GetFileContent(Guid id, string fileId, CancellationToken cancellationToken)
    {
        var file = await imagesService.GetFileContentAsync(id, fileId, cancellationToken);
        return file is null ? NotFound() : Ok(file);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await imagesService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(id), ex.Message);
            return ValidationProblem(ModelState);
        }
    }
}
