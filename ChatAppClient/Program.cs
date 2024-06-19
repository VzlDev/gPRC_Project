using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using ChatApp;
using Grpc.Core;

class Program
{
    static async Task Main(string[] args)
    {
        // Create a channel to the gRPC server
        var channel = GrpcChannel.ForAddress("https://localhost:7187");
        var client = new ChatService.ChatServiceClient(channel);

        Console.WriteLine("Enter your username:");
        var username = Console.ReadLine();

        Console.WriteLine("Enter the room you want to join:");
        var room = Console.ReadLine();

        // Join the specified room
        var joinResponse = await client.JoinRoomAsync(new JoinRoomRequest
        {
            Username = username,
            Room = room
        });

        if (joinResponse.Success)
        {
            Console.WriteLine($"Joined room: {room}");
        }
        else
        {
            Console.WriteLine("Failed to join room");
            return;
        }

        // Task for receiving messages
        var receiveTask = Task.Run(async () =>
        {
            using var call = client.ReceiveMessages(new ReceiveMessagesRequest { Room = room });
            await foreach (var message in call.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine($"[{message.Username}] {message.Message}");
            }
        });

        // Loop to send messages
        while (true)
        {
            var message = Console.ReadLine();
            if (message.ToLower() == "exit") break;

            var sendMessageResponse = await client.SendMessageAsync(new SendMessageRequest
            {
                Username = username,
                Room = room,
                Message = message
            });

            if (!sendMessageResponse.Success)
            {
                Console.WriteLine("Failed to send message");
            }
        }

        // Wait for the receiving task to complete
        await receiveTask;
    }
}
