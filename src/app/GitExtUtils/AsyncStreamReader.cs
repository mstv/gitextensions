using GitUI;
using Microsoft.VisualStudio.Threading;

namespace GitExtUtils;

public class AsyncStreamReader : IDisposable
{
    private const int _lineBufferSize = 64 * 1024;
    private static readonly TimeSpan _pollDelay = TimeSpan.FromMilliseconds(100);

    private bool _disposedValue;
    private CancellationTokenSource _cancellationTokenSource = new();
    private Action<string> _notify;
    private StreamReader _streamReader;
    private readonly TaskManager _taskManager = ThreadHelper.CreateTaskManager();

    public AsyncStreamReader(StreamReader streamReader, Action<string> notify)
    {
        _streamReader = streamReader;
        _notify = notify;

        CancellationToken cancellationToken = _cancellationTokenSource.Token;
        _taskManager.FileAndForget(async () =>
        {
            char[] buffer = new char[_lineBufferSize];
            string received = "";
            while (true)
            {
                try
                {
                    CancellationTokenSource readTimeoutTokenSource = new(_pollDelay);
                    int length = await _streamReader.ReadAsync(new Memory<char>(buffer), cancellationToken.CombineWith(readTimeoutTokenSource.Token).Token);
                    if (length == 0)
                    {
                        if (_streamReader.EndOfStream)
                        {
                            break;
                        }

                        continue;
                    }

                    received += new string(buffer, startIndex: 0, length);
                    int lastLineEnd = received.LastIndexOf('\n') + 1;
                    if (lastLineEnd > 0)
                    {
                        _notify(received[..lastLineEnd]);
                    }

                    received = received[lastLineEnd..];
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    if (received.Length > 0)
                    {
                        _notify(received);
                        received = "";
                    }
                }
            }
        });
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // dispose managed state (managed objects)
                CancelOperation();
            }

            _disposedValue = true;
        }
    }

    public void CancelOperation()
    {
        _cancellationTokenSource.Cancel();
    }

    public Task WaitUntilEofAsync(CancellationToken cancellationToken)
    {
        return _taskManager.JoinPendingOperationsAsync(cancellationToken);
    }
}
