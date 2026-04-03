using ObservableCollections;

namespace Bia.LogViewer.Core;

public interface ILogEntrySource
{
    IReadOnlyObservableList<LogModel>? Entries { get; }
}
