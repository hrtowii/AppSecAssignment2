using System.Configuration;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Assignment2.Models;
using Assignment2.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Assignment2.Controllers
{
    public class RegistrationController : Controller
    {
        private readonly MyDbContext _dbContext;
        private readonly EmailService _emailService;
        private readonly EncryptionService _encryptionService;
        private readonly IConfiguration _configuration;
        private readonly RedisService _redisService;

        public RegistrationController(MyDbContext dbContext, EmailService emailService, EncryptionService encryptionService, IConfiguration configuration, RedisService redisService)
        {
            _dbContext = dbContext;
            _emailService = emailService;
            _encryptionService = encryptionService;
            _configuration = configuration;
            _redisService = redisService;
        }

        // GET: /register (via custom route)
        public IActionResult Register()
        {
            return View();
        }

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Register(RegistrationViewModel model)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var isCaptchaValid = await IsCaptchaValid(model.Captcha);
    if (!isCaptchaValid)
    {
        ModelState.AddModelError("Captcha", "Captcha validation failed.");
        return View(model);
    }

    if (_dbContext.Users.Any(u => u.Email.ToLower() == model.Email.Trim().ToLower()))
    {
        ModelState.AddModelError("Email", "Email already exists.");
        return View(model);
    }

    // Process photo upload here
    string photoPath = null;
    if (model.Photo != null)
    {
        if (model.Photo.ContentType.ToLower() != "image/jpeg" && 
            model.Photo.ContentType.ToLower() != "image/jpg")
        {
            ModelState.AddModelError("Photo", "Invalid photo format. Only JPG allowed.");
            return View(model);
        }

        string imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        if (!Directory.Exists(imagesPath))
        {
            Directory.CreateDirectory(imagesPath);
        }

        string fileName = Path.GetFileName(model.Photo.FileName);
        string filePath = Path.Combine(imagesPath, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await model.Photo.CopyToAsync(stream);
        }
        photoPath = "/images/" + fileName;
    }
    else
    {
        ModelState.AddModelError("Photo", "Photo is required.");
        return View(model);
    }

    // Save registration data (excluding IFormFile) with photo path
    var registrationData = new RegistrationData
    {
        FullName = model.FullName,
        CreditCardNo = model.CreditCardNo,
        Gender = model.Gender,
        MobileNo = model.MobileNo,
        DeliveryAddress = model.DeliveryAddress,
        Email = model.Email,
        Password = model.Password,
        AboutMe = model.AboutMe,
        PhotoPath = photoPath
    };

    TempData["RegistrationData"] = JsonConvert.SerializeObject(registrationData);

    var otp = new Random().Next(100000, 1000000).ToString();
    HttpContext.Session.SetString("RegistrationOTP", otp);
    Console.WriteLine($"OTP: {otp}");
    Console.WriteLine($"Email: {model.Email}");
    try
    {
        await _emailService.SendEmailAsync(
            model.Email,
            "Account Verification",
            $"Hello,<br/><br/>Your verification code is: <strong>{otp}</strong>"
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        ModelState.AddModelError("", "Failed to send verification email. Please try again.");
        return View(model);
    }

    return RedirectToAction("VerifyOTP");
}

        // GET: /Registration/VerifyOTP  
        // Display a view (VerifyOTP.cshtml) with a field for the user to enter the OTP.
        public IActionResult VerifyOTP()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOTP(string otp)
        {
            string sessionOtp = HttpContext.Session.GetString("RegistrationOTP");

            if (string.IsNullOrEmpty(sessionOtp) || sessionOtp != otp)
            {
                ModelState.AddModelError("", "Invalid OTP.");
                return View();
            }

            if (TempData["RegistrationData"] == null)
            {
                ModelState.AddModelError("", "Registration details have expired, please register again.");
                return RedirectToAction("Register");
            }

            var jsonData = TempData["RegistrationData"].ToString();
            var registrationData = JsonConvert.DeserializeObject<RegistrationData>(jsonData);

            string encryptedCC = _encryptionService.Encrypt(registrationData.CreditCardNo);
            string passwordHash = _encryptionService.HashPassword(registrationData.Password, out string salt);

            var newUser = new User
            {
                FullName = registrationData.FullName,
                EncryptedCreditCardNo = encryptedCC,
                Gender = registrationData.Gender,
                MobileNo = registrationData.MobileNo,
                DeliveryAddress = registrationData.DeliveryAddress,
                Email = registrationData.Email,
                PasswordHash = passwordHash,
                Salt = salt,
                PhotoPath = registrationData.PhotoPath,
                AboutMe = registrationData.AboutMe,
                CreatedDate = DateTime.UtcNow,
                LastPasswordReset = DateTime.UtcNow
            };

            // try
            // {
                _dbContext.Users.Add(newUser);
                await _dbContext.SaveChangesAsync();
            // }
            // catch (Exception ex)
            // {
            //     Console.WriteLine(ex.Message);
            //     ModelState.AddModelError("", "An error occurred during registration. Please try again.");
            //     return View();
            // }

            HttpContext.Session.Remove("RegistrationOTP");
            return RedirectToAction("Login", "Registration");
        }
        

        // GET: /login (via custom route)
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
public async Task<IActionResult> Login(string email, string password)
{
    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
    {
        ModelState.AddModelError("", "Email and password are required.");
        return View();
    }

    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    if (user == null)
    {
        ModelState.AddModelError("", "Invalid credentials.");
        return View();
    }

    // Check account lockout
    if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
    {
        ModelState.AddModelError("", $"Account locked until {user.LockoutEnd.Value.ToLocalTime()}.");
        return View();
    }

    // Verify password
    bool isValid = _encryptionService.VerifyPassword(password, user.PasswordHash, user.Salt);
    if (!isValid)
    {
        user.FailedLoginAttempts++;
        if (user.FailedLoginAttempts >= 3)
        {
            user.LockoutEnd = DateTime.UtcNow.AddMinutes(1);
        }
        await _dbContext.SaveChangesAsync();
        ModelState.AddModelError("", "Invalid credentials.");
        return View();
    }

    // Check existing sessions
    // var sessionId = HttpContext.Session.Id;
    // var existingSessionId = await _redisService.GetAsync($"session:{user.Id}");
    // await _redisService.DeleteAsync($"session:{user.Id}");
    // if (!string.IsNullOrEmpty(existingSessionId) && existingSessionId != sessionId)
    // {
    //     ModelState.AddModelError("", "You are already logged in from another device.");
    //     return View();
    // }

    // Create claims
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email)
    };

    var claimsIdentity = new ClaimsIdentity(
        claims, 
        CookieAuthenticationDefaults.AuthenticationScheme);

    await HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(claimsIdentity),
        new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTime.UtcNow.AddMinutes(30),
            AllowRefresh = true
        });
    HttpContext.User = new ClaimsPrincipal(claimsIdentity);

    Console.WriteLine($"User authenticated: {User.Identity.IsAuthenticated}");
    Console.WriteLine($"Claims after sign-in: {string.Join(", ", User.Claims.Select(c => $"{c.Type}: {c.Value}"))}");
    // Update user state
    user.FailedLoginAttempts = 0;
    user.LockoutEnd = null;
    var newSessionId = HttpContext.Session.Id;
    Console.WriteLine($"New session ID: {newSessionId}");
    await _redisService.SetAsync($"session:{user.Id}", newSessionId);
    HttpContext.Session.SetString("SessionId", newSessionId); // Store in ASP.NET Core session    
    Console.WriteLine(_redisService.GetAsync($"session:{user.Id}"));
    Console.WriteLine($"Session ID stored in Redis for user {user.Id}: {newSessionId}");
    // Audit log
    _dbContext.AuditLogs.Add(new AuditLog
    {
        UserId = user.Id.ToString(),
        Action = "Login",
        Timestamp = DateTime.UtcNow,
        Details = "Successful login."
    });

    await _dbContext.SaveChangesAsync();

    return RedirectToAction("Index", "Home");
}
        public class RecaptchaResponse
        {
            [JsonProperty("success")]
            public bool Success { get; set; }
    
            [JsonProperty("score")]
            public float Score { get; set; }
        }
        private async Task<bool> IsCaptchaValid(string captchaResponse)
        {
            var secret = _configuration["GoogleRecaptcha:SecretKey"];
            using var client = new HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", secret),
                new KeyValuePair<string, string>("response", captchaResponse)
            });
    
            var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
    
            var result = JsonConvert.DeserializeObject<RecaptchaResponse>(responseString);
            return result.Success && result.Score >= 0.5; // Adjust score threshold as needed
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await _redisService.DeleteAsync($"session:{userId}");
    
            return RedirectToAction("Login", "Registration");
        }
        
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ModelState.AddModelError("", "No account found with this email.");
                return View();
            }

            // Generate a password reset token
            var token = Guid.NewGuid().ToString();
            user.ResetToken = token;
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            await _dbContext.SaveChangesAsync();

            // Send reset link via email
            var resetLink = Url.Action("ResetPassword", "Registration", new { token }, Request.Scheme);
            Console.WriteLine(resetLink);
            await _emailService.SendEmailAsync(
                user.Email,
                "Password Reset",
                $"Click <a href='{resetLink}'>here</a> to reset your password."
            );

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }
        
        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Error", "Home");
            }
            return View(new ResetPasswordViewModel { Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u =>
                    u.ResetToken == model.Token &&
                    u.ResetTokenExpiry > DateTime.UtcNow);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid or expired token.");
                return View(model);
            }
    
            // Enforce minimum password age (for example, 5 minutes)
            if (user.LastPasswordReset.HasValue &&
                DateTime.UtcNow < user.LastPasswordReset.Value.AddMinutes(5))
            {
                ModelState.AddModelError("", "Password was recently changed. Please wait a few minutes before resetting again.");
                return View(model);
            }
    
            // Prevent password reuse: the new password must not match the current one.
            if (_encryptionService.VerifyPassword(model.NewPassword, user.PasswordHash, user.Salt))
            {
                ModelState.AddModelError("", "The new password must be different from the current password.");
                return View(model);
            }
    
            // Compute and update the new password hash, update LastPasswordReset, and clear token fields.
            user.PasswordHash = _encryptionService.HashPassword(model.NewPassword, out string salt);
            user.Salt = salt;
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            user.LastPasswordReset = DateTime.UtcNow;
    
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error resetting password: {ex.Message}");
                ModelState.AddModelError("", "Error resetting password.");
                return View(model);
            }
            return RedirectToAction("ResetPasswordConfirmation");
        }

        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}