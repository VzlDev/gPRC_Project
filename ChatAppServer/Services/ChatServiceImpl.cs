using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatApp;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace ChatAppServer.Services
{
    public class ChatServiceImpl : ChatService.ChatServiceBase
    {
        private static readonly ConcurrentDictionary<string, List<IServerStreamWriter<ChatMessage>>> _rooms = new ConcurrentDictionary<string, List<IServerStreamWriter<ChatMessage>>>();

        public override Task<JoinRoomResponse> JoinRoom(JoinRoomRequest request, ServerCallContext context)
        {
            if (!_rooms.ContainsKey(request.Room))
            {
                _rooms[request.Room] = new List<IServerStreamWriter<ChatMessage>>();
            }
            return Task.FromResult(new JoinRoomResponse { Success = true });
        }

        public override Task<SendMessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
        {
            if (_rooms.TryGetValue(request.Room, out var clients))
            {
                var message = new ChatMessage
                {
                    Username = request.Username,
                    Message = request.Message,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                foreach (var client in clients)
                {
                    client.WriteAsync(message);
                }
            }
            return Task.FromResult(new SendMessageResponse { Success = true });
        }

        public override async Task ReceiveMessages(ReceiveMessagesRequest request, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
        {
            if (_rooms.TryGetValue(request.Room, out var clients))
            {
                clients.Add(responseStream);
                try
                {
                    await Task.Delay(Timeout.Infinite, context.CancellationToken);
                }
                finally
                {
                    clients.Remove(responseStream);
                }
            }
        }
    }
}
