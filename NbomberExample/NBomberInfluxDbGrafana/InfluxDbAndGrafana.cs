using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Sinks.InfluxDB;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NBomberInfluxDbGrafana
{
    /// <summary>
    /// to start, use:  docker-compose up -d
    /// to stop, use:  docker-compose down
    ///
    /// grafana default credentials: user:admin pass:admin
    /// Download grafana dashboard from: https://nbomber.com/docs/grafana-dashboard
    /// </summary>
    public class InfluxDbAndGrafana
    {
        [Test]
        public void ExampleInfluxDbTest()
        {
            using var httpClient = new HttpClient();

            // first, you need to create a step
            var step = Step.Create("step1-Call-Google", timeout: TimeSpan.FromSeconds(120), execute: async context =>
            {
                //context.Logger.Information("step 1 is invoked!!!");
                var response = await httpClient.GetAsync("https://nbomber.com/", context.CancellationToken);

                await Task.Delay(1000);

                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: (int)response.StatusCode)
                    : Response.Fail(statusCode: (int)response.StatusCode, error: response.StatusCode.ToString());
            });

            // second, we add our step to the scenario
            var scenario = ScenarioBuilder.CreateScenario("call_google", step)
                                            .WithLoadSimulations(Simulation.KeepConstant(copies: 1, TimeSpan.FromSeconds(30)));

            // InfluxDb
            InfluxDbSinkConfig influxConfig = InfluxDbSinkConfig.Create(url: "http://localhost:8086/",
                                                                    database: "loadTests", 
                                                                    userName: "tempUser",
                                                                    password: "tempPass");
            InfluxDBSink influxDb = new InfluxDBSink(influxConfig);

            var result = NBomberRunner.RegisterScenarios(scenario)
                                        .WithTestSuite("TestSuite-Example-InfluxDb")
                                        .WithTestName("Test-Example-InfluxDb")
                                        .WithReportingSinks(influxDb)
                                        .WithReportingInterval(TimeSpan.FromSeconds(5))
                                        .Run();

            var output = JsonConvert.SerializeObject(result);
            Console.WriteLine(output);

            Assert.IsTrue(result.RequestCount > 0);
            Assert.IsTrue(result.OkCount > 0);
            Assert.IsTrue(result.FailCount == 0);
        }
    }
}
