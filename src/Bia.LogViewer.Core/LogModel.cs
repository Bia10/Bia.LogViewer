using Microsoft.Extensions.Logging;

namespace Bia.LogViewer.Core;

public readonly record struct LogModel
{
    public DateTime Timestamp { get; init; }
    public LogLevel LogLevel { get; init; }
    public EventId EventId { get; init; }
    public string Message { get; init; }
    public string? Exception { get; init; }
}
