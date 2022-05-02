using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace GrpcServiceExample
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;

        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }

        public override Task<HelloReply> SayHello2(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.CalculateSize()
            });
        }

        public override async Task SayHelloStream(HelloRequest request, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
        {
            while (context.CancellationToken.IsCancellationRequested == false)
            {
                int count = 0;
                while (count < 10)// similando un trabajo pesado.
                {
                    Console.WriteLine("Server working " + count++);
                    await Task.Delay(1000, cancellationToken: context.CancellationToken);
                }
                
                if (context.CancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Token Cancelled WHILE");
                }

                var now = DateTime.Now.ToString("O");
                Console.WriteLine("Server sendig data "+ now);
                await responseStream.WriteAsync(new HelloReply
                {
                    Message = "Message time" + now
                });
                await Task.Delay(1000);
            }
        }
    }
}