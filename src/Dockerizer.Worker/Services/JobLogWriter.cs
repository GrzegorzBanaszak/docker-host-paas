namespace Dockerizer.Worker.Services;

public sealed class JobLogWriter
{
    public Task WriteLineAsync(string workspacePath, string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(workspacePath);

        var logPath = Path.Combine(workspacePath, "job.log");
        var line = $"[{DateTimeOffset.UtcNow:O}] {message}{Environment.NewLine}";
        return File.AppendAllTextAsync(logPath, line, cancellationToken);
    }
}
