using System.ComponentModel.DataAnnotations;

namespace Assignment2.Models;

public class ResetPasswordViewModel
{
    [Required]
    public string Token { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{12,}$", 
        ErrorMessage = "Password must be at least 12 characters with uppercase, lowercase, number, and special character")]
    public string NewPassword { get; set; }

    // [DataType(DataType.Password)]
    // [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    // public string ConfirmPassword { get; set; }
}