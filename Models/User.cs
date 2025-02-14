namespace Assignment2.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string EncryptedCreditCardNo { get; set; }
        public string Gender { get; set; }
        public string MobileNo { get; set; }
        public string DeliveryAddress { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; } // Add this
        public string PhotoPath { get; set; }
        public string AboutMe { get; set; }
        public DateTime CreatedDate { get; set; }
        public int FailedLoginAttempts { get; set; } // Add this
        public DateTime? LockoutEnd { get; set; } // Add this
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
    }
}