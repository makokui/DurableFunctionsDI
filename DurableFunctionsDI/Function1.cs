using DurableTask.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;


// See https://qiita.com/TsuyoshiUshio@github/items/03ca341414c74e46121d
namespace DurableFunctionsDI
{
    // public static class Function1
    public class Function1 : IFunction
    {
        //public Guid Id { get; } = Guid.NewGuid();


        // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-code-constraints?tabs=csharp
        [Function(nameof(RunOrchestrator))]
        //public static async Task<List<string>> RunOrchestrator(
        public async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(RunOrchestrator));
            //logger.LogInformation($"{this.GetType()}.RunOrchestrator, Guid:{this.Id}");
            logger.LogInformation("Started RunOrchestrator.");

            //logger.LogInformation("Saying hello.");

            //// Replace name and input with values relevant for your Durable Functions Activity
            List<Task<string>> tasks = [];
            string[] wordlists = await context.CallActivityAsync<string[]>(nameof(GetAllWords), "Tokyo, Seattle, London");  
            for (int i = 0; i < wordlists.Length; i++)
            {
                tasks.Add(context.CallActivityAsync<string>(nameof(SayHello), wordlists[i]));
            }

            await Task.WhenAll([.. tasks]);

            var outputs = new List<string>();
            foreach (Task<string> task in tasks)
            {
                outputs.Add(task.Result);
            }

            logger.LogInformation("Finished RunOrchestrator.");
            return outputs;
        }


        [Function(nameof(GetAllWords))]
        public async Task<string[]> GetAllWords([ActivityTrigger] string wordslist, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(GetAllWords));
            logger.LogInformation($"Started GetAllWords with '{wordslist}'.", wordslist);

            await Task.Delay(60000);

            logger.LogInformation($"Finished GetAllWords with '{wordslist}'.", wordslist);
            return wordslist.Split(',');
        }


        [Function(nameof(SayHello))]
        // public static string SayHello([ActivityTrigger] string name, FunctionContext executionContext)
        public async Task<string> SayHello([ActivityTrigger] string name, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("SayHello");
            logger.LogInformation($"Started SayHello with '{name}'.", name);
            //logger.LogInformation("Saying hello to {name}.", name);

            switch (name)
            {
                case "Tokyo":
                    await Task.Delay(30000);
                    break;
                case "Seattle":
                    await Task.Delay(10000);
                    break;
                case "London":
                    await Task.Delay(20000);
                    break;
                default:
                    break;
            }   

            logger.LogInformation($"Finished SayHello with '{name}'.", name);
            return $"Hello {name}!";
        }


        [Function("Function1_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("Function1_HttpStart");
            logger.LogInformation("Started Function1_HttpStart.");

            // Check if an instance with the specified ID already exists or an existing one stopped running(completed/failed/terminated).
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-singletons?tabs=csharp
            string instanceId = "makokui-durablefunctions-di_DurableFunctionsDI_Function1_RunOrchestrator";
            var existingInstance = await client.GetInstanceAsync(instanceId);
            if (existingInstance == null
            || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Completed
            || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Failed
            || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Terminated)
            {
                logger.LogInformation("Started orchestration with ID='{instanceId}' RuntimeStatus='{RuntimeStatus}'.", instanceId, existingInstance?.RuntimeStatus);

                try {
                    // Function input comes from the request content.
                    await client.ScheduleNewOrchestrationInstanceAsync(
                        nameof(Function1),
                        new StartOrchestrationOptions()
                        {
                            InstanceId = instanceId,
                        });
                }
                catch (Exception ex)
                {
                    logger.LogError($"{ex.Message}");
                    logger.LogError($"{ex.StackTrace}");
                }
        }
            else
            {
                // An instance with the specified ID exists or an existing one still running, don't create one.
                logger.LogWarning($"Skipped orchestration with ID = '{instanceId}' due to '{existingInstance.RuntimeStatus}'.");
            }

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration

            logger.LogInformation("Finished Function1_HttpStart.");
            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
