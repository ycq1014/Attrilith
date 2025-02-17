using Attrilith.Service;

namespace Simple.Services;

[HostedService]
public class TestHostService : IHostedService, IDisposable
{
    private bool _disposed;
    private bool _isRunning;
    private Timer _timer = null!;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_isRunning)
        {
            Console.WriteLine("TestHostService is already running.");
            return Task.CompletedTask;
        }

        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        _isRunning = true;

        Console.WriteLine("TestHostService has started.");

        return Task.CompletedTask;
    }

    private static void DoWork(object? state)
    {
        Console.WriteLine("TestHostService is working.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_isRunning)
        {
            Console.WriteLine("TestHostService is already running.");
            return Task.CompletedTask;
        }

        _timer.Change(Timeout.Infinite, 0);
        _isRunning = false;
        Console.WriteLine("TestHostService has stopped.");

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _timer.Dispose();
        }

        _disposed = true;
    }
}