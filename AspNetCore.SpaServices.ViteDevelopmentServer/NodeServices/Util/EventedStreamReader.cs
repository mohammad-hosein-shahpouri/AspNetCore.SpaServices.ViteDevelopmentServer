using System.Text;
using System.Text.RegularExpressions;

namespace AspNetCore.SpaServices.ViteDevelopmentServer.NodeServices.Util;

internal sealed class EventedStreamReader
{
    public delegate void OnReceivedChunkHandler(ArraySegment<char> chunk);

    public delegate void OnReceivedLineHandler(string line);

    public delegate void OnStreamClosedHandler();

    public event OnReceivedChunkHandler? OnReceivedChunk;

    public event OnReceivedLineHandler? OnReceivedLine;

    public event OnStreamClosedHandler? OnStreamClosed;

    private readonly StreamReader streamReader;
    private readonly StringBuilder linesBuffer;

    public EventedStreamReader(StreamReader streamReader)
    {
        this.streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));
        this.linesBuffer = new StringBuilder();
        Task.Factory.StartNew(Run, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    public Task<Match> WaitForMatch(Regex regex)
    {
        var tcs = new TaskCompletionSource<Match>();
        var completionLock = new object();

        OnReceivedLineHandler? onReceivedLineHandler = null;
        OnStreamClosedHandler? onStreamClosedHandler = null;

        void ResolveIfStillPending(Action applyResolution)
        {
            lock (completionLock)
            {
                if (!tcs.Task.IsCompleted)
                {
                    OnReceivedLine -= onReceivedLineHandler;
                    OnStreamClosed -= onStreamClosedHandler;
                    applyResolution();
                }
            }
        }

        onReceivedLineHandler = line =>
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                ResolveIfStillPending(() => tcs.SetResult(match));
            }
        };

        onStreamClosedHandler = () =>
        {
            ResolveIfStillPending(() => tcs.SetException(new EndOfStreamException()));
        };

        OnReceivedLine += onReceivedLineHandler;
        OnStreamClosed += onStreamClosedHandler;

        return tcs.Task;
    }

    private async Task Run()
    {
        var buf = new char[8 * 1024];
        while (true)
        {
            var chunkLength = await streamReader.ReadAsync(buf, 0, buf.Length);
            if (chunkLength == 0)
            {
                if (this.linesBuffer.Length > 0)
                {
                    OnCompleteLine(this.linesBuffer.ToString());
                    this.linesBuffer.Clear();
                }

                OnClosed();
                break;
            }

            OnChunk(new ArraySegment<char>(buf, 0, chunkLength));

            int lineBreakPos;
            var startPos = 0;

            // get all the newlines
            while ((lineBreakPos = Array.IndexOf(buf, '\n', startPos, chunkLength - startPos)) >= 0 && startPos < chunkLength)
            {
                var length = (lineBreakPos + 1) - startPos;
                this.linesBuffer.Append(buf, startPos, length);
                OnCompleteLine(this.linesBuffer.ToString());
                this.linesBuffer.Clear();
                startPos = lineBreakPos + 1;
            }

            // get the rest
            if (lineBreakPos < 0 && startPos < chunkLength)
            {
                this.linesBuffer.Append(buf, startPos, chunkLength - startPos);
            }
        }
    }

    private void OnChunk(ArraySegment<char> chunk)
    {
        var dlg = OnReceivedChunk;
        dlg?.Invoke(chunk);
    }

    private void OnCompleteLine(string line)
    {
        var dlg = OnReceivedLine;
        dlg?.Invoke(line);
    }

    private void OnClosed()
    {
        var dlg = OnStreamClosed;
        dlg?.Invoke();
    }
}