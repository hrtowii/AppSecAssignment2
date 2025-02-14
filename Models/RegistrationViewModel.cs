using System.ComponentModel.DataAnnotations;

namespace Assignment2.Models
{
    public class RegistrationViewModel
    {
        [Required(ErrorMessage = "Full Name is required")]
        public string FullName { get; set; }
        
        [Required(ErrorMessage = "Credit Card No is required")]
        [DataType(DataType.CreditCard)]
        public string CreditCardNo { get; set; }  // Will be encrypted later

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; }
        
        [Phone(ErrorMessage = "Invalid mobile number format.")]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = "Invalid international phone number.")]
        public string MobileNo { get; set; }
        
        [Required(ErrorMessage = "Delivery Address is required")]
        public string DeliveryAddress { get; set; }
        
        [Required(ErrorMessage = "Email Address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{12,}$", 
            ErrorMessage = "Password must be at least 12 characters long and include uppercase, lowercase, number, and special character.")]
        public string Password { get; set; }
        
        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
        
        [Required(ErrorMessage = "Please upload your photo (.JPG only)")]
        public IFormFile Photo { get; set; }
        
        [Required(ErrorMessage = "About Me is required")]
        [StringLength(500, ErrorMessage = "About Me cannot exceed 500 characters.")]
        public string AboutMe { get; set; }        
        [Required(ErrorMessage = "Captcha is required")]
        public string Captcha { get; set; } // For anti-bot validation
    }
}