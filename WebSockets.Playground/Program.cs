using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using WebSockets.Playground;

internal class Program
{
    public static ConcurrentDictionary<WebSocket, string> _socketClients = new ConcurrentDictionary<WebSocket, string>();

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var app = builder.Build();

        app.UseWebSockets();

        // Only works for a single connected device
        //app.Map("/ws", async (HttpContext context) =>
        //{
        //    if (context.WebSockets.IsWebSocketRequest)
        //    {
        //        using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

        //        var buffer = new byte[1024 * 4];
        //        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        //        while (!result.CloseStatus.HasValue)
        //        {
        //            string message = Encoding.UTF8.GetString(buffer, 0, result.Count);

        //            byte[] response = Encoding.UTF8.GetBytes($"Echo: {message}");
        //            await webSocket.SendAsync(new ArraySegment<byte>(response), result.MessageType, result.EndOfMessage, CancellationToken.None);

        //            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        //        }

        //        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        //    }
        //    else
        //    {
        //        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        //    }
        //});

        // Works between multiple clients, like a chat service
        //app.Use(async (context, next) =>
        //{
        //    if (context.Request.Path == "/ws")
        //    {
        //        if (context.WebSockets.IsWebSocketRequest)
        //        {
        //            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        //            _socketClients.TryAdd(webSocket, string.Empty);     // Add the new client

        //            await WebSocketLogic.Echo(webSocket);

        //            // Remove the client when done
        //            _socketClients.TryRemove(webSocket, out _);
        //        }
        //        else
        //        {
        //            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        //        }
        //    }
        //    else
        //    {
        //        await next(context);
        //    }

        //});

        app.Use(async (context, next) =>
        {
            if (context.Request.Path == "/ws")
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    var buffer = new byte[1024 * 4];
                    var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    _socketClients.TryAdd(webSocket, string.Empty);     // Add the new client

                    while (!receiveResult.CloseStatus.HasValue)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                        var jsonObj = JsonSerializer.Deserialize<SocketMessage>(message);

                        byte[] response = Encoding.UTF8.GetBytes($"{jsonObj.Username}: {jsonObj.Message}");
                        await webSocket.SendAsync(new ArraySegment<byte>(response), receiveResult.MessageType, receiveResult.EndOfMessage, CancellationToken.None);

                        receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    }

                    // Remove the client when done
                    _socketClients.TryRemove(webSocket, out _);
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                }
            }
            else
            {
                await next(context);
            }

        });

        app.Run();
    }
}