using Bia.LogViewer.Core;
using Microsoft.Extensions.Logging;

namespace Bia.LogViewer.Core.Test;

public class LogModelTests
{
    [Test]
    public async Task LogModel_DefaultValues_AreCorrect()
    {
        var model = new LogModel();

        await Assert.That(model.Timestamp).IsEqualTo(default(DateTime));
        await Assert.That(model.LogLevel).IsEqualTo(LogLevel.Trace);
        await Assert.That(model.Message).IsNull();
        await Assert.That(model.Exception).IsNull();
    }

    [Test]
    public async Task LogModel_InitProperties_RoundTrip()
    {
        var now = DateTime.UtcNow;
        var eventId = new EventId(42, "TestEvent");

        var model = new LogModel
        {
            Timestamp = now,
            LogLevel = LogLevel.Warning,
            EventId = eventId,
            Message = "Test message",
            Exception = "System.Exception: boom",
        };

        await Assert.That(model.Timestamp).IsEqualTo(now);
        await Assert.That(model.LogLevel).IsEqualTo(LogLevel.Warning);
        await Assert.That(model.EventId).IsEqualTo(eventId);
        await Assert.That(model.Message).IsEqualTo("Test message");
        await Assert.That(model.Exception).IsEqualTo("System.Exception: boom");
    }

    [Test]
    public async Task LogModel_Equality_WorksForRecordStruct()
    {
        var now = DateTime.UtcNow;
        var a = new LogModel
        {
            Timestamp = now,
            LogLevel = LogLevel.Error,
            Message = "same",
        };
        var b = new LogModel
        {
            Timestamp = now,
            LogLevel = LogLevel.Error,
            Message = "same",
        };

        await Assert.That(a).IsEqualTo(b);
    }
}
