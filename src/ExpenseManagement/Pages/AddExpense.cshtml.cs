using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;
using ExpenseManagement.Models;

namespace ExpenseManagement.Pages;

public class AddExpenseModel : PageModel
{
    private readonly ILogger<AddExpenseModel> _logger;
    private readonly IExpenseService _expenseService;

    public List<ExpenseCategory> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }

    [BindProperty]
    public decimal Amount { get; set; }
    
    [BindProperty]
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    
    [BindProperty]
    public int CategoryId { get; set; }
    
    [BindProperty]
    public string? Description { get; set; }

    public AddExpenseModel(ILogger<AddExpenseModel> logger, IExpenseService expenseService)
    {
        _logger = logger;
        _expenseService = expenseService;
    }

    public async Task OnGetAsync()
    {
        try
        {
            Categories = await _expenseService.GetAllCategoriesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading categories");
            ErrorMessage = "Unable to load categories from database";
            ErrorDetails = $"Error in AddExpenseModel.OnGetAsync: {ex.Message}";
            
            // Provide dummy data
            Categories = new List<ExpenseCategory>
            {
                new() { CategoryId = 1, CategoryName = "Travel", IsActive = true },
                new() { CategoryId = 2, CategoryName = "Meals", IsActive = true },
                new() { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
                new() { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
                new() { CategoryId = 5, CategoryName = "Other", IsActive = true }
            };
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        try
        {
            var request = new CreateExpenseRequest
            {
                UserId = 1, // Default user for demo
                CategoryId = CategoryId,
                Amount = Amount,
                Currency = "GBP",
                ExpenseDate = ExpenseDate,
                Description = Description
            };

            await _expenseService.CreateExpenseAsync(request);
            return RedirectToPage("/ViewExpenses");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            ErrorMessage = "Failed to create expense";
            ErrorDetails = $"Error: {ex.Message}";
            await OnGetAsync();
            return Page();
        }
    }
}
