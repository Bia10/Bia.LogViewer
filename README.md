# Bia.LogViewer

![.NET](https://img.shields.io/badge/net10.0-5C2D91?logo=.NET&labelColor=gray)
![C#](https://img.shields.io/badge/C%23-14.0-239120?labelColor=gray)
[![Build Status](https://github.com/Bia10/Bia.LogViewer/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/Bia10/Bia.LogViewer/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/Bia10/Bia.LogViewer/branch/main/graph/badge.svg)](https://codecov.io/gh/Bia10/Bia.LogViewer)
[![License](https://img.shields.io/github/license/Bia10/Bia.LogViewer)](https://github.com/Bia10/Bia.LogViewer/blob/main/LICENSE)

Reusable Avalonia log viewer control with filtering for .NET applications. Built with [SukiUI](https://github.com/kikipoulet/SukiUI) and [Material.Icons](https://github.com/SKProCH/Material.Icons).

Cross-platform, trimmable and AOT/NativeAOT compatible.

⭐ Please star this project if you like it. ⭐

[Usage](#usage) | [Development](#development)

## Packages

| Package | NuGet | Description |
| ------- | ----- | ----------- |
| **Bia.LogViewer.Core** | [![NuGet](https://img.shields.io/nuget/v/Bia.LogViewer.Core?color=purple)](https://www.nuget.org/packages/Bia.LogViewer.Core/) | Core abstractions: `LogModel`, `ILogEntrySource`, `IClipboardService` |
| **Bia.LogViewer.Avalonia** | [![NuGet](https://img.shields.io/nuget/v/Bia.LogViewer.Avalonia?color=purple)](https://www.nuget.org/packages/Bia.LogViewer.Avalonia/) | Avalonia `UserControl` + `LogViewerViewModel` with filtering UI |

All packages are cross-platform, trimmable and AOT/NativeAOT compatible.

## Features

- **Log level filtering** — toggle Info, Warning, Error, Critical independently via bitmask
- **Auto-scroll** — automatically scrolls to the latest log entry
- **Copy on select** — optionally copies selected log message to clipboard
- **Per-level counters** — real-time count badges for each severity level
- **Observable source** — uses `ObservableCollections` for high-performance reactive updates

## Usage

### 1. Implement the interfaces

```csharp
// Provide log entries from your logging pipeline
public class MyLogSource : ILogEntrySource
{
    public IReadOnlyObservableList<LogModel>? Entries { get; }
}

// Platform clipboard integration
public class MyClipboard : IClipboardService
{
    public async Task CopyToClipboardAsync(string? text)
    {
        // Use Avalonia's clipboard API
    }
}
```

### 2. Create the ViewModel and bind it

```csharp
var vm = new LogViewerViewModel(logSource, clipboardService);
```

### 3. Use the control in AXAML

```xml
<logViewer:LogViewerControl DataContext="{Binding LogViewerVm}" />
```

## Development

```shell
dotnet tool restore
dotnet build -c Release
dotnet test
dotnet csharpier format .
dotnet format style
dotnet format analyzers
```

## License

[MIT](LICENSE)
