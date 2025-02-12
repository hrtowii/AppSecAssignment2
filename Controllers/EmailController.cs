using Resend;

namespace Assignment2.Services
{
    public class EmailService
    {
        private readonly IResend _resend;
        private readonly string _fromEmail;

        public EmailService(IConfiguration configuration, IResend resend)
        {
            _resend = resend;
            _fromEmail = "verification@hrtowii.dev";
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var message = new EmailMessage
            {
                From = _fromEmail,
                To = toEmail,
                Subject = subject,
                HtmlBody = htmlContent
            };
            await _resend.EmailSendAsync(message);
        }
    }
}