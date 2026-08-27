using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;

namespace XCapture.Services;

public sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = @"Local\XCapture.SingleInstance";
    private const string ActivationPipeName = "XCapture.Activation";

    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _cancellation = new();
    private Task? _listenerTask;
    private bool _ownsMutex;

    public bool IsPrimaryInstance { get; }

    public event Action<IReadOnlyList<string>>? ActivationRequested;

    public SingleInstanceService()
    {
        _mutex = new Mutex(true, MutexName, out var createdNew);
        IsPrimaryInstance = createdNew;
        _ownsMutex = createdNew;

    }

    public void StartListening()
    {
        if (!IsPrimaryInstance || _listenerTask is not null)
        {
            return;
        }

        _listenerTask = Task.Run(async () =>
        {
            while (!_cancellation.IsCancellationRequested)
            {
                try
                {
                    await using var pipe = new NamedPipeServerStream(
                        ActivationPipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                    await pipe.WaitForConnectionAsync(_cancellation.Token);
                    using var reader = new StreamReader(pipe);
                    var payload = await reader.ReadToEndAsync(_cancellation.Token);
                    var arguments = JsonSerializer.Deserialize<string[]>(payload) ?? [];
                    ActivationRequested?.Invoke(arguments);
                }
                catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exc)
                {
                    LogService.Error(exc, "Single instance activation listener failed");
                }
            }
        });
    }

    public void RequestActivation(IEnumerable<string> arguments)
    {
        if (IsPrimaryInstance)
        {
            return;
        }

        try
        {
            using var pipe = new NamedPipeClientStream(".", ActivationPipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly);
            pipe.Connect(3000);
            using var writer = new StreamWriter(pipe) { AutoFlush = true };
            writer.Write(JsonSerializer.Serialize(arguments.ToArray()));
        }
        catch (Exception exc)
        {
            LogService.Error(exc, "Failed to forward activation to primary instance");
        }
    }

    public void Dispose()
    {
        _cancellation.Cancel();

        if (_ownsMutex)
        {
            _mutex.ReleaseMutex();
            _ownsMutex = false;
        }

        _mutex.Dispose();
        _cancellation.Dispose();
    }
}
