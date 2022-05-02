using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcServiceExample;

namespace GrpcClienteExample
{
    class Program
    {
        /// <summary>
        /// Este main es para probar viendo como escribe en la consola en tiempo real. Pues los test se demora en
        ///     escribir en la consola y no se ve bien la intecaccion de cliente y server para los casos de stream.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            var test = new ClientExamples();
            await test.ServerStreamCall();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
