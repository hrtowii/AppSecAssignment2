using System;

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
        
        public string PhotoPath { get; set; }
        
        public string AboutMe { get; set; }
        
        public DateTime CreatedDate { get; set; }
    }
}