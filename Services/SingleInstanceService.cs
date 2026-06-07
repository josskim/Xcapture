using System.Threading;

namespace XCapture.Services;

public sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = @"Local\XCapture.SingleInstance";
    private const string ActivationEventName = @"Local\XCapture.Activate";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle? _activationEvent;
    private readonly CancellationTokenSource _cancellation = new();
    private Task? _listenerTask;
    private bool _ownsMutex;

    public bool IsPrimaryInstance { get; }

    public event Action? ActivationRequested;

    public SingleInstanceService()
    {
        _mutex = new Mutex(true, MutexName, out var createdNew);
        IsPrimaryInstance = createdNew;
        _ownsMutex = createdNew;

        if (createdNew)
        {
            _activationEvent = new EventWaitHandle(
                false,
                EventResetMode.AutoReset,
                ActivationEventName);
            return;
        }

        SignalPrimaryInstance();
    }

    public void StartListening()
    {
        if (!IsPrimaryInstance || _activationEvent is null || _listenerTask is not null)
        {
            return;
        }

        _listenerTask = Task.Run(() =>
        {
            while (!_cancellation.IsCancellationRequested)
            {
                _activationEvent.WaitOne();
                if (!_cancellation.IsCancellationRequested)
                {
                    ActivationRequested?.Invoke();
                }
            }
        });
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _activationEvent?.Set();

        if (_ownsMutex)
        {
            _mutex.ReleaseMutex();
            _ownsMutex = false;
        }

        _activationEvent?.Dispose();
        _mutex.Dispose();
        _cancellation.Dispose();
    }

    private static void SignalPrimaryInstance()
    {
        try
        {
            using var activationEvent = EventWaitHandle.OpenExisting(ActivationEventName);
            activationEvent.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // The primary process is still completing startup.
        }
    }
}
