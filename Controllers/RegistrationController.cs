using Microsoft.AspNetCore.Mvc;
using Assignment2.Models;
using Assignment2.Services;
using System.IO;

namespace Assignment2.Controllers
{
    public class RegistrationController : Controller
    {
        private readonly MyDbContext _dbContext;

        public RegistrationController(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: /register (via custom route)
        public IActionResult Register()
        {
            return View();
        }

        // POST: /register
        [HttpPost]
        [ValidateAntiForgeryToken] 
        public IActionResult Register(RegistrationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Return validation errors.
                return View(model);
            }

            // Example server-side captcha check.
            if (!IsCaptchaValid(model.Captcha))
            {
                ModelState.AddModelError("Captcha", "Invalid captcha response.");
                return View(model);
            }

            // Encrypt credit card number.
            string encryptedCC = EncryptionService.Encrypt(model.CreditCardNo);

            // Create a new User entity.
            var newUser = new User
            {
                FullName = model.FullName,
                EncryptedCreditCardNo = encryptedCC,
                Gender = model.Gender,
                MobileNo = model.MobileNo,
                DeliveryAddress = model.DeliveryAddress,
                Email = model.Email,
                // In production, use a proper password hashing algorithm.
                PasswordHash = EncryptionService.Encrypt(model.Password),
                AboutMe = model.AboutMe,
                CreatedDate = DateTime.UtcNow
            };

            // Process the photo upload (only JPG allowed).
            if (model.Photo != null && model.Photo.ContentType.ToLower() == "image/jpeg")
            {
                // Ensure wwwroot/images folder exists.
                string imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }
                string fileName = Path.GetFileName(model.Photo.FileName);
                string filePath = Path.Combine(imagesPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.Photo.CopyTo(stream);
                }
                newUser.PhotoPath = "/images/" + fileName;
            }
            else
            {
                ModelState.AddModelError("Photo", "Invalid photo format. Only JPG allowed.");
                return View(model);
            }

            // Additional server-side validations (e.g., uniqueness of email) can be added here.
            // For this example, try to add user to DB.
            try
            {
                // Optionally check if email already exists.
                if (_dbContext.Users.Any(u => u.Email == newUser.Email))
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                    return View(model);
                }

                _dbContext.Users.Add(newUser);
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log exception and display a friendly error message.
                ModelState.AddModelError("", "An error occurred while processing your registration. Please try again later.");
                return View(model);
            }

            // Redirect to the Login view upon successful registration.
            return RedirectToAction("Login");
        }

        // GET: /login (via custom route)
        public IActionResult Login()
        {
            return View();
        }

        // POST: /login (for demonstration only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string Email, string Password)
        {
            // Authenticate user (example only; replace with proper logic).
            // In this demo, we simply return the login view.
            return View();
        }

        // Pseudo-code for captcha validation.
        private bool IsCaptchaValid(string captchaResponse)
        {
            // Replace with real captcha validation (e.g., via external API or session-based).
            return true;
        }
    }
}