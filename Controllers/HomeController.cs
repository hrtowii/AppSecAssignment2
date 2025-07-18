using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Assignment2.Models;
using Assignment2.Services;
using Microsoft.EntityFrameworkCore;

namespace Assignment2.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly MyDbContext _dbContext;
    private readonly EncryptionService _encryptionService;

    public HomeController(
        ILogger<HomeController> logger,
        MyDbContext dbContext,
        EncryptionService encryptionService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _encryptionService = encryptionService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine($"User ID from claim: {userId}");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Registration");
        }
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));

        if (user == null)
        {
            return NotFound();
        }

        // Decrypt credit card information
        var decryptedCard = _encryptionService.Decrypt(user.EncryptedCreditCardNo);

        var viewModel = new UserViewModel
        {
            FullName = user.FullName,
            CreditCardNo = decryptedCard,
            Gender = user.Gender,
            MobileNo = user.MobileNo,
            DeliveryAddress = user.DeliveryAddress,
            Email = user.Email,
            PhotoPath = user.PhotoPath,
            AboutMe = user.AboutMe
        };

        return View(viewModel);
    }
    
    public IActionResult Error(int? statusCode = null)
    {
        var errorModel = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = statusCode
        };

        if (statusCode.HasValue)
        {
            switch (statusCode.Value)
            {
                case 404:
                    errorModel.Details = "Page not found (404).";
                    break;
                case 403:
                    errorModel.Details = "You are not authorized to view this page (403).";
                    break;
                default:
                    errorModel.Details = "An unexpected error occurred.";
                    break;
            }
        }
        return View("Error", errorModel);
    }
}