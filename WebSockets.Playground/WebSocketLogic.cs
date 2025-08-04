using System.Net.WebSockets;
using System.Text;

namespace WebSockets.Playground
{
    public static class WebSocketLogic
    {
        public static async Task Echo(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            var receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!receiveResult.CloseStatus.HasValue)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                await BroadcastMessageAsync(message, webSocket);

                receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(
                receiveResult.CloseStatus.Value,
                receiveResult.CloseStatusDescription,
                CancellationToken.None);
        }

        private static async Task BroadcastMessageAsync(string message, WebSocket sender)
        {
            byte[] response = Encoding.UTF8.GetBytes($"Echo: {message}");

            foreach (var client in Program._socketClients.Keys)
            {
                if (client != sender) // Don't send the message back to the sender
                {
                    await client.SendAsync(
                        new ArraySegment<byte>(response),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
            } 
        }
    }
}
