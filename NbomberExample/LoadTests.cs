using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcServiceExample;
using NBomber.Contracts;
using NBomber.CSharp;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net.Http;
using NBomber.Configuration;

namespace NbomberExample
{
    public class LoadTests
    {
        /// <summary>
        /// Steps run sequentially
        /// Scenarios run in parallel
        /// Simulations run sequentially
        ///
        /// https://nbomber.com/docs/general-concepts
        /// https://nbomber.com/docs/test-automation/
        /// https://github.com/PragmaticFlow/NBomber
        /// https://nbomber.com/
        /// 
        /// </summary>
        [Test]
        public void LoadTestHttp()
        {
            using var httpClient = new HttpClient();

            // first, you need to create a step
            var step = Step.Create("step1-Call-Google", timeout: TimeSpan.FromMilliseconds(300), execute:async context =>
            {
                // you can define and execute any logic here,
                // for example: send http request, SQL query etc
                // NBomber will measure how much time it takes to execute your logic


                //context.Logger.Information("step 1 is invoked!!!");
                var response = await httpClient.GetAsync("https://www.google.com/", context.CancellationToken);

                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: (int)response.StatusCode)
                    : Response.Fail(statusCode: (int)response.StatusCode, error: response.StatusCode.ToString());
            });

            // second, we add our step to the scenario
            var scenario = ScenarioBuilder.CreateScenario("hello_world_Scenario", step)
                                            .WithWarmUpDuration(TimeSpan.FromSeconds(5))
                                            //.WithLoadSimulations(Simulation.InjectPerSec(copies:2, TimeSpan.FromSeconds(30)));
                                            .WithLoadSimulations(Simulation.KeepConstant(copies:2, TimeSpan.FromSeconds(10)));
                                            //.WithLoadSimulations(Simulation.RampPerSec(rate:5, TimeSpan.FromSeconds(20)));

            var result = NBomberRunner.RegisterScenarios(scenario)

                                .WithTestSuite("TestSuite-Name")// default value is: nbomber_default_test_suite_name.
                                .WithTestName("Test-Name")// default value is: "nbomber_report_{current-date}".
                                
                                //.WithReportFileName("my_report_name")// default name: nbomber_report
                                //.WithReportFolder("my_report_name")// default folder path: "./reports".
                                //.WithReportFormats(ReportFormat.Txt, ReportFormat.Csv, ReportFormat.Html, ReportFormat.Md)
                                //.WithoutReports() - use it when you don't need a report file

                                .Run();

            var output = JsonConvert.SerializeObject(result);
            Console.WriteLine(output);

            Assert.IsTrue(result.FailCount == 0);

            var stepResult = result.ScenarioStats.First(x => x.ScenarioName == "hello_world_Scenario").StepStats.First(s =>s.StepName == "step1-Call-Google");
            Assert.IsTrue(stepResult.Ok.Request.RPS > 10);
            Assert.IsTrue(stepResult.Ok.Latency.Percent99 < 100);

            //https://nbomber.com/docs/test-automation/
            //stats.RequestCount > 10_000 // all request count
            //stats.OkCount > 10_000      // ok request count 
            //stats.FailCount = 0         // fail request count
            //stats.Min < 50              // min response latency
            //stats.Mean < 100            // mean response latency
            //stats.Max < 200             // max response latency
            //stats.RPS = 1_000           // request per second count
            //stats.Percent50 < 50        // 50% of response latency
            //stats.Percent75 < 100       // 75% of response latency
            //stats.Percent95 < 100       // 95% of response latency
            //stats.Percent99 < 100       // 99% of response latency
            //stats.MinDataKb < 50.0      // min item size that transferred during scenario
            //stats.MeanDataKb < 50.0     // mean item size that transferred during scenario
            //stats.MaxDataKb < 50.0      // max item size item that transferred during scenario
            //stats.AllDataMB < 50.0      // all data that transferred during scenario 
        }

        [Test]
        public void LoadTestGrpc()
        {
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new Greeter.GreeterClient(channel);
            var request = new HelloRequest() { DeliveryDate = DateTime.UtcNow.ToTimestamp(), Name = "SomeName" };

            const string stepName = "Step-call-Grpc";
            const string scenarioName = "Scenario-LoadTest";

            var step = Step.Create(stepName, timeout: TimeSpan.FromMilliseconds(300), execute: async context =>
            {
                try
                {
                    var response = await client.SayHelloAsync(request);
                    return Response.Ok(statusCode: (int)200);
                }
                catch (RpcException e)
                {
                    context.Logger.Error("Grpc Error "+e.Message, e);
                    return Response.Fail(statusCode: (int)e.StatusCode, error: e.StatusCode.ToString());
                }
                catch (Exception e)
                {
                    context.Logger.Error("Error calling API");
                    return Response.Fail(statusCode: (int)-500, error: e.Message);
                }
            });

            var scenario = ScenarioBuilder.CreateScenario(scenarioName, step)
                                            .WithWarmUpDuration(TimeSpan.FromSeconds(5))
                                            .WithLoadSimulations(Simulation.KeepConstant(copies: 2, TimeSpan.FromSeconds(10)));

            var result = NBomberRunner
                                .RegisterScenarios(scenario)
                                .WithReportFormats(ReportFormat.Html)
                                .Run();

            Assert.IsTrue(result.FailCount == 0);
            var stepResult = result.ScenarioStats.First(x => x.ScenarioName == scenarioName).StepStats.First(s => s.StepName == stepName);
            Assert.IsTrue(stepResult.Ok.Request.RPS > 10);
            Assert.IsTrue(stepResult.Ok.Latency.Percent99 < 100);
        }
    }
}
