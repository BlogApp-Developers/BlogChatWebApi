using System.Net.WebSockets;

namespace BlogChat.WebSocket;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

public class WebSocketHandler
{
    private static readonly ConcurrentDictionary<string, WebSocket> _webSockets = new ConcurrentDictionary<string, WebSocket>();

    public async Task HandleWebSocketConnections(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var userId = context.Request.Query["userId"].ToString(); 

            if (string.IsNullOrEmpty(userId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            _webSockets.TryAdd(userId, webSocket);
            await ProcessWebSocketMessages(userId, webSocket);

            _webSockets.TryRemove(userId, out _);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    private async Task ProcessWebSocketMessages(string userId, WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!result.CloseStatus.HasValue)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

            if (message.StartsWith("typing:"))
            {
                var typingUser = message.Substring(7);
                await BroadcastMessage($"{typingUser} is typing...", userId);
            }
            else
            {
                await BroadcastMessage(message, userId);
            }

            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }

    private async Task BroadcastMessage(string message, string senderUserId)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message);

        foreach (var pair in _webSockets)
        {
            var receiverUserId = pair.Key;
            var receiverWebSocket = pair.Value;

            if (receiverUserId != senderUserId)
            {
                await receiverWebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
