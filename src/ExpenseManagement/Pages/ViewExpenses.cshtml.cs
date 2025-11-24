using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;
using ExpenseManagement.Models;

namespace ExpenseManagement.Pages;

public class ViewExpensesModel : PageModel
{
    private readonly ILogger<ViewExpensesModel> _logger;
    private readonly IExpenseService _expenseService;

    public List<Expense> Expenses { get; set; } = new();
    public string? Filter { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }

    public ViewExpensesModel(ILogger<ViewExpensesModel> logger, IExpenseService expenseService)
    {
        _logger = logger;
        _expenseService = expenseService;
    }

    public async Task OnGetAsync(string? filter)
    {
        Filter = filter;
        
        try
        {
            var allExpenses = await _expenseService.GetAllExpensesAsync();
            
            if (!string.IsNullOrWhiteSpace(filter))
            {
                Expenses = allExpenses.Where(e => 
                    e.CategoryName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    e.Description?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true ||
                    e.StatusName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }
            else
            {
                Expenses = allExpenses;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading expenses");
            ErrorMessage = "Unable to load expenses from database";
            ErrorDetails = $"Error in ViewExpensesModel.OnGetAsync: {ex.Message}";
            
            // Provide dummy data
            Expenses = new List<Expense>
            {
                new() { 
                    ExpenseId = 1, 
                    ExpenseDate = DateTime.Today.AddDays(-5), 
                    CategoryName = "Travel", 
                    AmountDecimal = 120.00m, 
                    StatusName = "Submitted",
                    Description = "Train ticket to client site"
                },
                new() { 
                    ExpenseId = 2, 
                    ExpenseDate = DateTime.Today.AddDays(-3), 
                    CategoryName = "Meals", 
                    AmountDecimal = 69.00m, 
                    StatusName = "Submitted",
                    Description = "Client dinner"
                },
                new() { 
                    ExpenseId = 3, 
                    ExpenseDate = DateTime.Today.AddDays(-10), 
                    CategoryName = "Supplies", 
                    AmountDecimal = 99.50m, 
                    StatusName = "Approved",
                    Description = "Office supplies"
                },
                new() { 
                    ExpenseId = 4, 
                    ExpenseDate = DateTime.Today.AddDays(-2), 
                    CategoryName = "Travel", 
                    AmountDecimal = 19.20m, 
                    StatusName = "Approved",
                    Description = "Taxi fare"
                }
            };
        }
    }

    public async Task<IActionResult> OnPostSubmitAsync(int expenseId)
    {
        try
        {
            await _expenseService.SubmitExpenseAsync(expenseId);
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense {ExpenseId}", expenseId);
            ErrorMessage = "Failed to submit expense";
            ErrorDetails = $"Error: {ex.Message}";
            await OnGetAsync(null);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int expenseId)
    {
        try
        {
            await _expenseService.DeleteExpenseAsync(expenseId);
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense {ExpenseId}", expenseId);
            ErrorMessage = "Failed to delete expense";
            ErrorDetails = $"Error: {ex.Message}";
            await OnGetAsync(null);
            return Page();
        }
    }
}
