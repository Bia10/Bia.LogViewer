namespace Bia.LogViewer.Core;

public interface IClipboardService
{
    Task CopyToClipboardAsync(string? text);
}
