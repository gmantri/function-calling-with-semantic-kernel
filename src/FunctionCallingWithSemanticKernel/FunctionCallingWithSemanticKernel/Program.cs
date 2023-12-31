using Azure.AI.OpenAI;
using FunctionCallingWithSemanticKernel.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;

// Azure OpenAI settings. You can get these settings from portal.
const string AZURE_OPEN_AI_ENDPOINT = "<your-azure-openai-endpoint like https://xyz.openai.azure.com/>";
const string AZURE_OPEN_AI_KEY = "<your-azure-openai-key like 567bf9aa8caf4b4eb3b3c3b42f8fc745>";
const string AZURE_OPEN_AI_DEPLOYMENT_ID = "<your-azure-openai-deployment-id like gpt-4-32k>";

// create an instance of OpenAIClient.
var openAIClient = new OpenAIClient(new Uri(AZURE_OPEN_AI_ENDPOINT), new Azure.AzureKeyCredential(AZURE_OPEN_AI_KEY));

// get the kernel.
var kernel = GetKernel();

// set OpenAI prompt execution settings.
var promptExecutionSettings = new OpenAIPromptExecutionSettings()
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
    Temperature = 0
};
Console.WriteLine("Hello, I am an AI assistant that can answer simple math questions.");
Console.WriteLine("Please ask me questions like \"What is 2 x 2\" or \"What is sqaure root of 3\" etc.");
Console.WriteLine("To quit, simply type quit.");
Console.WriteLine("");
Console.WriteLine("Now ask me a math question, I am waiting!");
do
{
    var prompt = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(prompt))
    {
        if (prompt.ToLowerInvariant() == "quit")
        {
            Console.WriteLine("Thank you! See you next time.");
            break;
        }
        else
        {
            // get the tool/function best suited to execute the function.
            var function = await SelectTool(prompt);
            if (function != null)
            {
                // now we try to get the plugin function and the arguments.
                kernel.Plugins.TryGetFunctionAndArguments(function, out KernelFunction? pluginFunction,
                    out KernelArguments? arguments);
                // execute the plugin function.
                var result = await kernel.InvokeAsync(pluginFunction!, arguments);
                Console.WriteLine($"{prompt}: {result.ToString()}");
            }
            else
            {
                Console.WriteLine("I'm sorry but I am not able to answer your question. I can only answer simple math questions.");
            }
        }
    }
} while (true);


// select the tool best suited to execute our prompt.
async Task<OpenAIFunctionToolCall?> SelectTool(string prompt)
{
    try
    {
        var chatCompletionService = new AzureOpenAIChatCompletionService(AZURE_OPEN_AI_DEPLOYMENT_ID, openAIClient!);
        var result = await chatCompletionService.GetChatMessageContentAsync(new ChatHistory(prompt),
            new OpenAIPromptExecutionSettings()
            {
                ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions,
                Temperature = 0
            }, kernel);
        var functionCall = ((OpenAIChatMessageContent)result).GetOpenAIFunctionToolCalls().FirstOrDefault();

        return functionCall;
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        Console.WriteLine(ex.StackTrace);
        return null;
    }
}

// create an instance of Kernel and load all plugins and functions in the Kernel.
Kernel GetKernel()
{
    var kernelBuilder = Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(AZURE_OPEN_AI_DEPLOYMENT_ID, openAIClient);
	
    var kernel = kernelBuilder.Build();
    kernel.Plugins.AddFromType<MathPlugin>();
    return kernel;
}