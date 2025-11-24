using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Core;
using ExpenseManagement.Models;
using System.Text.Json;

namespace ExpenseManagement.Services;

public interface IChatService
{
    Task<string> GetChatResponseAsync(string userMessage, List<string> conversationHistory);
}

public class ChatService : IChatService
{
    private readonly IConfiguration _configuration;
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(IConfiguration configuration, IExpenseService expenseService, ILogger<ChatService> logger)
    {
        _configuration = configuration;
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task<string> GetChatResponseAsync(string userMessage, List<string> conversationHistory)
    {
        var openAIEndpoint = _configuration["OpenAI__Endpoint"];
        var deploymentName = _configuration["OpenAI__DeploymentName"];

        // Check if GenAI is configured
        if (string.IsNullOrEmpty(openAIEndpoint) || string.IsNullOrEmpty(deploymentName))
        {
            return GetDummyResponse(userMessage);
        }

        try
        {
            // Use ManagedIdentityCredential with explicit client ID
            var managedIdentityClientId = _configuration["ManagedIdentityClientId"];
            TokenCredential credential;
            
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                _logger.LogInformation("Using ManagedIdentityCredential with client ID");
                credential = new ManagedIdentityCredential(managedIdentityClientId);
            }
            else
            {
                _logger.LogInformation("Using DefaultAzureCredential");
                credential = new DefaultAzureCredential();
            }

            var client = new OpenAIClient(new Uri(openAIEndpoint), credential);

            // Build messages list
            var messages = new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage(GetSystemPrompt())
            };

            // Add conversation history
            foreach (var msg in conversationHistory.Take(10)) // Limit history
            {
                messages.Add(new ChatRequestUserMessage(msg));
            }

            messages.Add(new ChatRequestUserMessage(userMessage));

            // Create chat completions options with function definitions
            var chatOptions = new ChatCompletionsOptions(deploymentName, messages);
            
            // Add function definitions for tool calling
            foreach (var function in GetFunctionDefinitions())
            {
                chatOptions.Tools.Add(function);
            }

            var response = await client.GetChatCompletionsAsync(chatOptions);
            
            // Handle function calls if present
            if (response.Value.Choices[0].FinishReason == CompletionsFinishReason.ToolCalls)
            {
                foreach (var toolCall in response.Value.Choices[0].Message.ToolCalls)
                {
                    var functionCall = toolCall as ChatCompletionsFunctionToolCall;
                    if (functionCall != null)
                    {
                        var functionResult = await ExecuteFunctionAsync(functionCall.Name, functionCall.Arguments);
                        messages.Add(new ChatRequestAssistantMessage(response.Value.Choices[0].Message));
                        messages.Add(new ChatRequestToolMessage(functionResult, functionCall.Id));
                    }
                }

                // Get final response after function execution
                chatOptions = new ChatCompletionsOptions(deploymentName, messages);
                response = await client.GetChatCompletionsAsync(chatOptions);
            }

            return response.Value.Choices[0].Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Azure OpenAI");
            return GetDummyResponse(userMessage);
        }
    }

    private string GetSystemPrompt()
    {
        return @"You are a helpful assistant for the Expense Management System. You can help users:
- View all expenses or filter by status
- Get pending expenses awaiting approval
- Create new expenses
- Submit expenses for approval
- Approve or reject expenses
- Get expense categories and statuses
- Answer questions about the expense system

When displaying lists, format them clearly with bullets or numbers.
Always be professional and helpful.";
    }

    private List<ChatCompletionsFunctionToolDefinition> GetFunctionDefinitions()
    {
        return new List<ChatCompletionsFunctionToolDefinition>
        {
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "get_all_expenses",
                Description = "Retrieves all expenses from the database"
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "get_pending_expenses",
                Description = "Retrieves expenses that are pending approval"
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "get_categories",
                Description = "Retrieves all expense categories"
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "create_expense",
                Description = "Creates a new expense",
                Parameters = BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""amount"": { ""type"": ""number"", ""description"": ""Expense amount in GBP"" },
                        ""categoryId"": { ""type"": ""integer"", ""description"": ""Category ID (1=Travel, 2=Meals, 3=Supplies, 4=Accommodation, 5=Other)"" },
                        ""description"": { ""type"": ""string"", ""description"": ""Expense description"" },
                        ""expenseDate"": { ""type"": ""string"", ""description"": ""Date in ISO format"" }
                    },
                    ""required"": [""amount"", ""categoryId"", ""expenseDate""]
                }")
            }
        };
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, string arguments)
    {
        try
        {
            switch (functionName)
            {
                case "get_all_expenses":
                    var expenses = await _expenseService.GetAllExpensesAsync();
                    return JsonSerializer.Serialize(expenses.Select(e => new
                    {
                        e.ExpenseId,
                        e.ExpenseDate,
                        e.CategoryName,
                        Amount = $"£{e.AmountDecimal:N2}",
                        e.StatusName,
                        e.Description
                    }));

                case "get_pending_expenses":
                    var pending = await _expenseService.GetPendingExpensesAsync();
                    return JsonSerializer.Serialize(pending.Select(e => new
                    {
                        e.ExpenseId,
                        e.ExpenseDate,
                        e.CategoryName,
                        Amount = $"£{e.AmountDecimal:N2}",
                        e.UserName,
                        e.Description
                    }));

                case "get_categories":
                    var categories = await _expenseService.GetAllCategoriesAsync();
                    return JsonSerializer.Serialize(categories);

                case "create_expense":
                    var createRequest = JsonSerializer.Deserialize<CreateExpenseRequest>(arguments);
                    if (createRequest != null)
                    {
                        createRequest.UserId = 1; // Default user
                        createRequest.Currency = "GBP";
                        var expenseId = await _expenseService.CreateExpenseAsync(createRequest);
                        return JsonSerializer.Serialize(new { success = true, expenseId });
                    }
                    return JsonSerializer.Serialize(new { success = false, error = "Invalid request" });

                default:
                    return JsonSerializer.Serialize(new { error = "Unknown function" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private string GetDummyResponse(string userMessage)
    {
        var lowerMessage = userMessage.ToLowerInvariant();

        if (lowerMessage.Contains("expenses") || lowerMessage.Contains("show") || lowerMessage.Contains("list"))
        {
            return @"**Sample Expenses:**

1. Travel - £120.00 (Submitted)
2. Meals - £69.00 (Submitted)
3. Supplies - £99.50 (Approved)
4. Travel - £19.20 (Approved)

**Note**: Azure OpenAI services are not currently deployed. To enable AI-powered function calling and natural language queries, please run:
```bash
./deploy-with-chat.sh
```

For now, please use the regular UI pages at:
- /AddExpense - Create new expenses
- /ViewExpenses - View and manage expenses
- /ApproveExpenses - Approve pending expenses";
        }

        if (lowerMessage.Contains("pending") || lowerMessage.Contains("approval"))
        {
            return @"**Pending Expenses:**

1. £120.00 - Travel (20/01/2024) by Alice Example
2. £99.50 - Office Supplies (14/12/2023) by Alice Example

**Note**: Azure OpenAI services are not deployed. Run `./deploy-with-chat.sh` to enable AI features.";
        }

        if (lowerMessage.Contains("create") || lowerMessage.Contains("add") || lowerMessage.Contains("new expense"))
        {
            return @"To create a new expense, please visit:
**/AddExpense**

Or provide details in this format:
""Create an expense for £50 for travel on 2024-01-15 for taxi fare""

**Note**: Azure OpenAI services are not deployed. Run `./deploy-with-chat.sh` to enable natural language expense creation.";
        }

        if (lowerMessage.Contains("categories") || lowerMessage.Contains("category"))
        {
            return @"**Expense Categories:**

1. Travel
2. Meals
3. Supplies
4. Accommodation
5. Other

**Note**: Azure OpenAI services are not deployed. Run `./deploy-with-chat.sh` to enable full AI integration.";
        }

        return @"Hello! I'm your Expense Management AI Assistant. 

I can help you with:
- Viewing all expenses
- Checking pending approvals
- Creating new expenses  
- Getting expense categories

**Note**: Azure OpenAI services are not currently deployed. To enable AI-powered chat, please run:
```bash
./deploy-with-chat.sh
```

For now, please use the regular UI pages to manage your expenses.";
    }
}
