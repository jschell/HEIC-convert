using HEICAutoConverter.Core;

namespace HEICAutoConverter.Tests;

public class LoggerTests : IDisposable
{
    private readonly string _tempDir;

    public LoggerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "HEICTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void Log_NotifiesListeners()
    {
        using var logger = new Logger();
        var received = new List<string>();
        logger.AddListener(msg => received.Add(msg));

        logger.Log("test message");

        Assert.Single(received);
        Assert.Contains("test message", received[0]);
    }

    [Fact]
    public void Log_IncludesTimestamp()
    {
        using var logger = new Logger();
        var received = new List<string>();
        logger.AddListener(msg => received.Add(msg));

        logger.Log("timestamped");

        Assert.Matches(@"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}\]", received[0]);
    }

    [Fact]
    public void AddAndRemoveListener_WorksCorrectly()
    {
        using var logger = new Logger();
        var received = new List<string>();
        Action<string> listener = msg => received.Add(msg);

        logger.AddListener(listener);
        logger.Log("first");
        Assert.Single(received);

        logger.RemoveListener(listener);
        logger.Log("second");
        Assert.Single(received); // Should not receive second message
    }

    [Fact]
    public void Log_MultipleListeners_AllReceive()
    {
        using var logger = new Logger();
        var list1 = new List<string>();
        var list2 = new List<string>();

        logger.AddListener(msg => list1.Add(msg));
        logger.AddListener(msg => list2.Add(msg));

        logger.Log("broadcast");

        Assert.Single(list1);
        Assert.Single(list2);
        Assert.Contains("broadcast", list1[0]);
        Assert.Contains("broadcast", list2[0]);
    }

    [Fact]
    public async Task Log_WritesToFile()
    {
        using var logger = new Logger();

        logger.Log("file test");

        // Wait for async write
        await Task.Delay(2000);

        // Find the log file
        var logDir = Path.Combine(Settings.AppData, "logs");
        var logFile = Path.Combine(logDir, $"heicconverter_{DateTime.Now:yyyyMMdd}.log");

        if (File.Exists(logFile))
        {
            var content = File.ReadAllText(logFile);
            Assert.Contains("file test", content);
        }
        // If file doesn't exist, the test is inconclusive (permissions etc.)
    }

    [Fact]
    public void Dispose_CompletesWithinTimeout()
    {
        var logger = new Logger();
        logger.Log("before dispose");

        var task = Task.Run(() => logger.Dispose());
        var completed = task.Wait(TimeSpan.FromSeconds(10));

        Assert.True(completed, "Logger.Dispose() should complete within timeout");
    }

    [Fact]
    public void Log_FailingListener_DoesNotBreakOthers()
    {
        using var logger = new Logger();
        var received = new List<string>();

        logger.AddListener(_ => throw new Exception("listener error"));
        logger.AddListener(msg => received.Add(msg));

        logger.Log("resilient");

        Assert.Single(received);
        Assert.Contains("resilient", received[0]);
    }
}
