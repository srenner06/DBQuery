using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Microsoft.Extensions.Logging;

namespace DBQuery.Tests;

public class SqliteContainer : IDatabaseContainer
{
    private readonly string _filePath;

    public SqliteContainer()
    {
        // Creates a temp in-memory database. Replace with a file path if you want file-based persistence.
        _filePath = ":memory:";
    }

    public string GetConnectionString() => $"Data Source={_filePath};";

    public Task StartAsync(CancellationToken ct = default)
    {
        // No actual container to start for SQLite
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        // Nothing to dispose for in-memory DB
        await Task.CompletedTask;
    }

    #region Unused Members - Dummy Implementations

    public DateTime CreatedTime => DateTime.MinValue;
    public DateTime StartedTime => DateTime.MinValue;
    public DateTime StoppedTime => DateTime.MinValue;
    public DateTime PausedTime => DateTime.MinValue;
    public DateTime UnpausedTime => DateTime.MinValue;

    public ILogger Logger => throw new NotSupportedException("Logger is not supported for SQLite container.");
    public string Id => "sqlite-container";
    public string Name => "sqlite";
    public string IpAddress => "127.0.0.1";
    public string MacAddress => "00:00:00:00:00:00";
    public string Hostname => "localhost";

    public IImage Image => throw new NotSupportedException("No image for SQLite.");
    public TestcontainersStates State => TestcontainersStates.Running;
    public TestcontainersHealthStatus Health => TestcontainersHealthStatus.Healthy;
    public long HealthCheckFailingStreak => 0;

#pragma warning disable CS0067 // needed becaus of interface
    public event EventHandler? Creating;
    public event EventHandler? Starting;
    public event EventHandler? Stopping;
    public event EventHandler? Pausing;
    public event EventHandler? Unpausing;
    public event EventHandler? Created;
    public event EventHandler? Started;
    public event EventHandler? Stopped;
    public event EventHandler? Paused;
    public event EventHandler? Unpaused;
#pragma warning restore CS0067

    public Task CopyAsync(byte[] fileContent, string filePath, UnixFileModes fileMode = UnixFileModes.UserRead, CancellationToken ct = default) => Task.CompletedTask;
    public Task CopyAsync(string source, string target, UnixFileModes fileMode = UnixFileModes.UserRead, CancellationToken ct = default) => Task.CompletedTask;
    public Task CopyAsync(DirectoryInfo source, string target, UnixFileModes fileMode = UnixFileModes.UserRead, CancellationToken ct = default) => Task.CompletedTask;
    public Task CopyAsync(FileInfo source, string target, UnixFileModes fileMode = UnixFileModes.UserRead, CancellationToken ct = default) => Task.CompletedTask;

    public Task<ExecResult> ExecAsync(IList<string> command, CancellationToken ct = default) => Task.FromResult(new ExecResult());
    public Task<long> GetExitCodeAsync(CancellationToken ct = default) => Task.FromResult(0L);
    public Task<(string Stdout, string Stderr)> GetLogsAsync(DateTime since = default, DateTime until = default, bool timestampsEnabled = true, CancellationToken ct = default) => Task.FromResult(("", ""));
    public ushort GetMappedPublicPort(int containerPort) => 0;
    public ushort GetMappedPublicPort(string containerPort) => 0;
    public Task PauseAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task<byte[]> ReadFileAsync(string filePath, CancellationToken ct = default) => Task.FromResult(Array.Empty<byte>());
    public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task UnpauseAsync(CancellationToken ct = default) => Task.CompletedTask;

    #endregion
}