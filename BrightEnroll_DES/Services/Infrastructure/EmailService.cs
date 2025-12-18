using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Infrastructure;

public interface IEmailService
{
    Task<bool> SendVerificationCodeAsync(string toEmail, string verificationCode, string recipientName = "");
    Task<bool> SendPasswordResetConfirmationAsync(string toEmail, string recipientName = "");
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService>? _logger;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly bool _enableSsl;
    private readonly string _fromAddress;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration, ILogger<EmailService>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger;

        // Get email settings from configuration
        _smtpHost = _configuration["Email:Smtp:Host"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(_configuration["Email:Smtp:Port"] ?? "587");
        _smtpUsername = _configuration["Email:Smtp:Username"] ?? "joshvanderson01@gmail.com";
        _smtpPassword = _configuration["Email:Smtp:Password"] ?? "ywanfatijyqdmpze";
        _enableSsl = bool.Parse(_configuration["Email:Smtp:EnableSsl"] ?? "true");
        _fromAddress = _configuration["Email:FromAddress"] ?? "joshvanderson01@gmail.com";
        _fromName = _configuration["Email:FromName"] ?? "BrightEnroll System";
    }

    public async Task<bool> SendVerificationCodeAsync(string toEmail, string verificationCode, string recipientName = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger?.LogWarning("Cannot send email: recipient email is empty");
                return false;
            }

            var displayName = string.IsNullOrWhiteSpace(recipientName) ? "User" : recipientName;
            
            var subject = "BrightEnroll - Password Reset Verification Code";
            var body = GetVerificationCodeEmailBody(displayName, verificationCode);

            return await SendEmailAsync(toEmail, subject, body);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error sending verification code email to {Email}: {Message}", toEmail, ex.Message);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetConfirmationAsync(string toEmail, string recipientName = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger?.LogWarning("Cannot send email: recipient email is empty");
                return false;
            }

            var displayName = string.IsNullOrWhiteSpace(recipientName) ? "User" : recipientName;
            
            var subject = "BrightEnroll - Password Reset Successful";
            var body = GetPasswordResetConfirmationEmailBody(displayName);

            return await SendEmailAsync(toEmail, subject, body);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error sending password reset confirmation email to {Email}: {Message}", toEmail, ex.Message);
            return false;
        }
    }

    private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = _enableSsl
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_fromAddress, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            
            _logger?.LogInformation("Email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send email to {Email}: {Message}", toEmail, ex.Message);
            return false;
        }
    }

    private string GetVerificationCodeEmailBody(string recipientName, string verificationCode)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset Verification</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <table role='presentation' style='width: 100%; border-collapse: collapse;'>
        <tr>
            <td style='padding: 20px 0; text-align: center; background-color: #0040B6;'>
                <h1 style='color: #ffffff; margin: 0; font-size: 24px; font-weight: bold;'>BrightEnroll</h1>
            </td>
        </tr>
        <tr>
            <td style='padding: 40px 20px; background-color: #ffffff;'>
                <div style='max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #333333; margin: 0 0 20px 0; font-size: 20px;'>Password Reset Verification</h2>
                    <p style='color: #666666; margin: 0 0 20px 0; font-size: 14px; line-height: 1.6;'>
                        Hello {recipientName},
                    </p>
                    <p style='color: #666666; margin: 0 0 20px 0; font-size: 14px; line-height: 1.6;'>
                        We received a request to reset your password for your BrightEnroll account. Use the verification code below to proceed:
                    </p>
                    <div style='background-color: #f0f0f0; border: 2px dashed #0040B6; border-radius: 8px; padding: 20px; text-align: center; margin: 30px 0;'>
                        <p style='color: #0040B6; margin: 0; font-size: 32px; font-weight: bold; letter-spacing: 5px; font-family: monospace;'>
                            {verificationCode}
                        </p>
                    </div>
                    <p style='color: #666666; margin: 20px 0 0 0; font-size: 14px; line-height: 1.6;'>
                        This code will expire in 10 minutes. If you didn't request this password reset, please ignore this email or contact our support team.
                    </p>
                    <p style='color: #666666; margin: 20px 0 0 0; font-size: 14px; line-height: 1.6;'>
                        Best regards,<br>
                        <strong>The BrightEnroll Team</strong>
                    </p>
                </div>
            </td>
        </tr>
        <tr>
            <td style='padding: 20px; text-align: center; background-color: #f5f5f5;'>
                <p style='color: #999999; margin: 0; font-size: 12px;'>
                    © {DateTime.Now.Year} BrightEnroll. All rights reserved.
                </p>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GetPasswordResetConfirmationEmailBody(string recipientName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset Successful</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <table role='presentation' style='width: 100%; border-collapse: collapse;'>
        <tr>
            <td style='padding: 20px 0; text-align: center; background-color: #0040B6;'>
                <h1 style='color: #ffffff; margin: 0; font-size: 24px; font-weight: bold;'>BrightEnroll</h1>
            </td>
        </tr>
        <tr>
            <td style='padding: 40px 20px; background-color: #ffffff;'>
                <div style='max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #333333; margin: 0 0 20px 0; font-size: 20px;'>Password Reset Successful</h2>
                    <p style='color: #666666; margin: 0 0 20px 0; font-size: 14px; line-height: 1.6;'>
                        Hello {recipientName},
                    </p>
                    <p style='color: #666666; margin: 0 0 20px 0; font-size: 14px; line-height: 1.6;'>
                        Your password has been successfully reset. You can now log in to your BrightEnroll account using your new password.
                    </p>
                    <div style='background-color: #e8f5e9; border-left: 4px solid #4caf50; padding: 15px; margin: 20px 0;'>
                        <p style='color: #2e7d32; margin: 0; font-size: 14px; font-weight: bold;'>
                            ✓ Password reset completed successfully
                        </p>
                    </div>
                    <p style='color: #666666; margin: 20px 0 0 0; font-size: 14px; line-height: 1.6;'>
                        If you did not perform this action, please contact our support team immediately.
                    </p>
                    <p style='color: #666666; margin: 20px 0 0 0; font-size: 14px; line-height: 1.6;'>
                        Best regards,<br>
                        <strong>The BrightEnroll Team</strong>
                    </p>
                </div>
            </td>
        </tr>
        <tr>
            <td style='padding: 20px; text-align: center; background-color: #f5f5f5;'>
                <p style='color: #999999; margin: 0; font-size: 12px;'>
                    © {DateTime.Now.Year} BrightEnroll. All rights reserved.
                </p>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}
