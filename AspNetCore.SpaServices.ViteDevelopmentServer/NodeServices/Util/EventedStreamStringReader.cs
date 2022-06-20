using System.Text;

namespace AspNetCore.SpaServices.ViteDevelopmentServer.NodeServices.Util;

internal sealed class EventedStreamStringReader : IDisposable
{
    private readonly EventedStreamReader eventedStreamReader;
    private bool isDisposed;
    private readonly StringBuilder stringBuilder = new StringBuilder();

    public EventedStreamStringReader(EventedStreamReader eventedStreamReader)
    {
        this.eventedStreamReader = eventedStreamReader
            ?? throw new ArgumentNullException(nameof(eventedStreamReader));
        this.eventedStreamReader.OnReceivedLine += OnReceivedLine;
    }

    public string ReadAsString() => stringBuilder.ToString();

    private void OnReceivedLine(string line) => stringBuilder.AppendLine(line);

    public void Dispose()
    {
        if (!isDisposed)
        {
            eventedStreamReader.OnReceivedLine -= OnReceivedLine;
            isDisposed = true;
        }
    }
}