namespace Assignment2.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public int? StatusCode { get; set; }
        public string? Details { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}