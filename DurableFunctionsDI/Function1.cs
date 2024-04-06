using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Runtime.CompilerServices;


namespace DurableFunctionsDI
{
    public interface IFunction
    {
        string Id { get; set; }
    }

    // public static class Function1
    public class Function1 : IFunction
    {
        public string Id { get; set; }

        public Function1()
        {
            this.Id = Guid.NewGuid().ToString();
        }


        [Function(nameof(Function1))]
        //public static async Task<List<string>> RunOrchestrator(
        public async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(Function1));
            logger.LogInformation($"Type: {this.GetType()}, Id: {this.Id}");
            logger.LogInformation("Saying hello.");
            var outputs = new List<string>();

            // Replace name and input with values relevant for your Durable Functions Activity
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }


        [Function(nameof(SayHello))]
        // public static string SayHello([ActivityTrigger] string name, FunctionContext executionContext)
        public string SayHello([ActivityTrigger] string name, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("SayHello");
            logger.LogInformation("Saying hello to {name}.", name);
            return $"Hello {name}!";
        }


        [Function("Function1_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("Function1_HttpStart");

            // Check if an instance with the specified ID already exists or an existing one stopped running(completed/failed/terminated).
            string instanceId = "MyInstanceId";
            var status = await client.GetInstanceAsync(instanceId);
            if (status == null || status.IsCompleted)
            {
                // Function input comes from the request content.
                //string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(Function1));
                await client.ScheduleNewOrchestrationInstanceAsync(
                    nameof(Function1)
                    , new StartOrchestrationOptions()
                    {
                        InstanceId = instanceId,
                    });

                logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

                // Returns an HTTP 202 response with an instance management payload.
                // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
                //return client.CreateCheckStatusResponse(req, instanceId);
            }
            else
            {
                // An instance with the specified ID exists or an existing one still running, don't create one.
                //return new HttpResponseMessage(HttpStatusCode.Conflict)
                //{
                //    Content = new StringContent($"An instance with ID '{instanceId}' already exists."),
                //};
                var reason = status.RuntimeStatus.ToString();
                logger.LogWarning("Skipped orchestration with ID = '{instanceId}'.", instanceId);
            }

            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
