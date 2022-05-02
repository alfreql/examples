using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcServiceExample;
using NUnit.Framework;

namespace GrpcClienteExample
{
    public class ClientExamples
    {
        [Test]
        public async Task UnaryCall()
        {
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new Greeter.GreeterClient(channel);

            var request = new HelloRequest() { DeliveryDate = DateTime.UtcNow.ToTimestamp(), Name = "SomeName" };
            var reply = await client.SayHelloAsync(request);
            Console.WriteLine("Greeting: " + reply.Message);
        }


        [Test]
        public async Task ServerStreamCall()
        {
            CancellationTokenSource cts = null;
            CancellationTokenSource cts1 = null;
            try
            {
                using var channel = GrpcChannel.ForAddress("https://localhost:5001");
                var client = new Greeter.GreeterClient(channel);

                var request = new HelloRequest { DeliveryDate = DateTime.UtcNow.ToTimestamp(), Name = "SomeName" };
                cts = new CancellationTokenSource(TimeSpan.FromSeconds(330));

                using AsyncServerStreamingCall<HelloReply> call = client.SayHelloStream(request, cancellationToken: cts.Token);

                Console.WriteLine("Client Ready to receive data ");

                cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(27));
                await foreach (var item in call.ResponseStream.ReadAllAsync(cts1.Token))
                {
                    Console.WriteLine("Message recived " + item.Message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("cts " + cts.IsCancellationRequested);
                Console.WriteLine("cts1 " + cts1.IsCancellationRequested);
            }

            Console.WriteLine("End Client...");
        }
    }
}
