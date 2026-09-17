using System.Collections.Concurrent;

namespace RengaMcp.Infrastructure;

internal sealed class StaWorker : IDisposable
{
    private readonly BlockingCollection<IWorkItem> _queue = new();
    private readonly Thread _thread;
    private bool _disposed;

    public StaWorker(string name)
    {
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = name
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    public Task<T> InvokeAsync<T>(Func<T> action, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(action);

        var item = new WorkItem<T>(action, cancellationToken);
        _queue.Add(item, cancellationToken);
        return item.Task;
    }

    public Task InvokeAsync(Action action, CancellationToken cancellationToken = default) =>
        InvokeAsync(
            () =>
            {
                action();
                return true;
            },
            cancellationToken);

    private void Run()
    {
        foreach (var item in _queue.GetConsumingEnumerable())
        {
            item.Execute();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _queue.CompleteAdding();
        _thread.Join(TimeSpan.FromSeconds(5));
        _queue.Dispose();
    }

    private interface IWorkItem
    {
        void Execute();
    }

    private sealed class WorkItem<T>(Func<T> action, CancellationToken cancellationToken) : IWorkItem
    {
        private readonly TaskCompletionSource<T> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<T> Task => _completion.Task;

        public void Execute()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _completion.TrySetCanceled(cancellationToken);
                return;
            }

            try
            {
                _completion.TrySetResult(action());
            }
            catch (Exception exception)
            {
                _completion.TrySetException(exception);
            }
        }
    }
}
