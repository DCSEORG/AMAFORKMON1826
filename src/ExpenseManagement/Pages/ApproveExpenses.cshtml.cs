using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;
using ExpenseManagement.Models;

namespace ExpenseManagement.Pages;

public class ApproveExpensesModel : PageModel
{
    private readonly ILogger<ApproveExpensesModel> _logger;
    private readonly IExpenseService _expenseService;

    public List<Expense> PendingExpenses { get; set; } = new();
    public string? Filter { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }

    public ApproveExpensesModel(ILogger<ApproveExpensesModel> logger, IExpenseService expenseService)
    {
        _logger = logger;
        _expenseService = expenseService;
    }

    public async Task OnGetAsync(string? filter)
    {
        Filter = filter;
        
        try
        {
            var pendingExpenses = await _expenseService.GetPendingExpensesAsync();
            
            if (!string.IsNullOrWhiteSpace(filter))
            {
                PendingExpenses = pendingExpenses.Where(e => 
                    e.CategoryName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    e.Description?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true ||
                    e.UserName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }
            else
            {
                PendingExpenses = pendingExpenses;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pending expenses");
            ErrorMessage = "Unable to load pending expenses from database";
            ErrorDetails = $"Error in ApproveExpensesModel.OnGetAsync: {ex.Message}";
            
            // Provide dummy data
            PendingExpenses = new List<Expense>
            {
                new() { 
                    ExpenseId = 1, 
                    ExpenseDate = new DateTime(2024, 1, 20), 
                    CategoryName = "Travel", 
                    AmountDecimal = 120.00m, 
                    StatusName = "Submitted",
                    UserName = "Alice Example",
                    Description = "Train tickets"
                },
                new() { 
                    ExpenseId = 2, 
                    ExpenseDate = new DateTime(2023, 12, 14), 
                    CategoryName = "Supplies", 
                    AmountDecimal = 99.50m, 
                    StatusName = "Submitted",
                    UserName = "Alice Example",
                    Description = "Office supplies"
                }
            };
        }
    }

    public async Task<IActionResult> OnPostApproveAsync(int expenseId)
    {
        try
        {
            // Using reviewer ID = 2 (Bob Manager) as default
            await _expenseService.ApproveExpenseAsync(expenseId, 2);
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense {ExpenseId}", expenseId);
            ErrorMessage = "Failed to approve expense";
            ErrorDetails = $"Error: {ex.Message}";
            await OnGetAsync(null);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRejectAsync(int expenseId)
    {
        try
        {
            // Using reviewer ID = 2 (Bob Manager) as default
            await _expenseService.RejectExpenseAsync(expenseId, 2);
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense {ExpenseId}", expenseId);
            ErrorMessage = "Failed to reject expense";
            ErrorDetails = $"Error: {ex.Message}";
            await OnGetAsync(null);
            return Page();
        }
    }
}
