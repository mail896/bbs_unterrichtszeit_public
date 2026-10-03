using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
namespace Unterrichtszeiten;
internal sealed class PtbClock
{
    private readonly object gate=new();
    private DateTimeOffset? anchor;
    private long stamp;
    private bool fresh;
    public bool Fresh { get { lock(gate)return fresh; } }
    public DateTimeOffset Now { get { lock(gate)return anchor is { } t?t+Stopwatch.GetElapsedTime(stamp):DateTimeOffset.UtcNow; } }
    public string Source { get { lock(gate)return fresh&&Stopwatch.GetElapsedTime(stamp)<TimeSpan.FromMinutes(6)?"PTB-ZEIT":anchor!=null?"ZEIT FORTGEFÜHRT":"GERÄTEZEIT"; } }
    public async Task Sync(CancellationToken lifetime)
    {
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(lifetime);timeout.CancelAfter(TimeSpan.FromSeconds(8));
        try
        {
            using var socket=new ClientWebSocket();socket.Options.AddSubProtocol("time");
            await socket.ConnectAsync(new Uri("wss://uhr.ptb.de/time"),timeout.Token);
            long sent=Stopwatch.GetTimestamp();const int token=1;
            await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"c\":1}")),WebSocketMessageType.Text,true,timeout.Token);
            var buffer=new byte[4096];int length=0;WebSocketReceiveResult reply;
            do{reply=await socket.ReceiveAsync(new ArraySegment<byte>(buffer,length,buffer.Length-length),timeout.Token);length+=reply.Count;if(reply.MessageType!=WebSocketMessageType.Text||length>=buffer.Length)throw new IOException("Ungültige Zeitantwort");}while(!reply.EndOfMessage);
            var elapsed=Stopwatch.GetElapsedTime(sent);using var json=JsonDocument.Parse(buffer.AsMemory(0,length));
            var root=json.RootElement;double ms=root.GetProperty("s").GetDouble();
            if(root.GetProperty("c").GetInt32()!=token||!double.IsFinite(ms)||ms<1577836800000||ms>4102444800000||elapsed.TotalSeconds>3)throw new IOException("Ungültiger Zeitwert");
            lock(gate){anchor=DateTimeOffset.FromUnixTimeMilliseconds((long)ms)+TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds/2);stamp=Stopwatch.GetTimestamp();fresh=true;}
        }
        catch(Exception ex) when(ex is WebSocketException or OperationCanceledException or IOException or JsonException or InvalidOperationException or KeyNotFoundException or FormatException){lock(gate)fresh=false;}
    }
}
