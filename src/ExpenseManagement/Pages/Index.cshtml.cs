using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IExpenseService _expenseService;

    public int TotalExpenses { get; set; }
    public int PendingExpenses { get; set; }
    public int ApprovedExpenses { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }

    public IndexModel(ILogger<IndexModel> logger, IExpenseService expenseService)
    {
        _logger = logger;
        _expenseService = expenseService;
    }

    public async Task OnGetAsync()
    {
        try
        {
            var allExpenses = await _expenseService.GetAllExpensesAsync();
            TotalExpenses = allExpenses.Count;
            PendingExpenses = allExpenses.Count(e => e.StatusName == "Submitted");
            ApprovedExpenses = allExpenses.Count(e => e.StatusName == "Approved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard statistics");
            
            // Show dummy data with error message
            TotalExpenses = 10;
            PendingExpenses = 3;
            ApprovedExpenses = 5;
            
            ErrorMessage = "Unable to connect to database";
            ErrorDetails = $"Error in IndexModel.OnGetAsync: {ex.Message}. ";
            
            if (ex.Message.Contains("managed identity") || ex.Message.Contains("authentication"))
            {
                ErrorDetails += "This appears to be a Managed Identity authentication issue. Please ensure: " +
                              "1) The managed identity is assigned to the App Service, " +
                              "2) The managed identity has db_datareader, db_datawriter, and EXECUTE permissions on the database, " +
                              "3) The AZURE_CLIENT_ID environment variable is set to the managed identity's client ID.";
            }
        }
    }
}
