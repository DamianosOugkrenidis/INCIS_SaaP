// Author: Damianos Pythagoras Ougkrenidis
//
using System.Net;
using System.Net.Mail;
using System.Text;

namespace INCIS_SaaP.Services;

/// <summary>
/// Service για αποστολή emails
/// </summary>
public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Αποστολή email
    /// </summary>
    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
            var smtpUsername = _configuration["Email:SmtpUsername"] ?? "";
            var smtpPassword = _configuration["Email:SmtpPassword"] ?? "";
            var fromEmail = _configuration["Email:FromEmail"] ?? "noreply@incis.local";
            var fromName = _configuration["Email:FromName"] ?? "INCIS SaaP";

            // Graceful skip όταν δεν έχει ρυθμιστεί SMTP (π.χ. dev/CI) — αποφυγή timeout/θορύβου
            if (string.IsNullOrWhiteSpace(smtpUsername) || string.IsNullOrWhiteSpace(smtpPassword))
            {
                _logger.LogWarning("Email not sent to {To}: SMTP δεν έχει ρυθμιστεί (Email:SmtpUsername/SmtpPassword).", to);
                return false;
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                Timeout = 10000
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml,
                BodyEncoding = Encoding.UTF8
            };
            message.To.Add(to);

            await client.SendMailAsync(message);

            _logger.LogInformation("Email sent to {To} with subject {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }

    /// <summary>
    /// Αποστολή email επαλήθευσης
    /// </summary>
    public async Task<bool> SendVerificationEmailAsync(string email, string userName, string verificationToken)
    {
        var verificationUrl = $"{_configuration["App:Url"]}/account/verify-email?token={verificationToken}";
        
        var body = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background: linear-gradient(135deg, #1a237e, #00acc1); color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 20px; background: #f9f9f9; }}
                    .button {{ display: inline-block; padding: 12px 24px; background: #1a237e; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
                    .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>INCIS SaaP</h1>
                    </div>
                    <div class='content'>
                        <h2>Επαλήθευση Email</h2>
                        <p>Αγαπητέ/ή {userName},</p>
                        <p>Ευχαριστούμε που εγγραφήκατε στο INCIS SaaP. Παρακαλώ επαληθεύστε το email σας κάνοντας κλικ στο παρακάτω κουμπί:</p>
                        <p style='text-align: center;'>
                            <a href='{verificationUrl}' class='button'>Επαλήθευση Email</a>
                        </p>
                        <p>Αν το κουμπί δεν λειτουργεί, μπορείτε να αντιγράψετε τον παρακάτω σύνδεσμο στο πρόγραμμα περιήγησής σας:</p>
                        <p>{verificationUrl}</p>
                        <p>Ο σύνδεσμος θα λήξει σε 24 ώρες.</p>
                    </div>
                    <div class='footer'>
                        <p>Αυτό είναι ένα αυτοματοποιημένο μήνυμα. Παρακαλώ μην απαντήσετε σε αυτό το email.</p>
                    </div>
                </div>
            </body>
            </html>";

        return await SendEmailAsync(email, "Επαλήθευση Email - INCIS SaaP", body);
    }

    /// <summary>
    /// Αποστολή email επαναφοράς κωδικού
    /// </summary>
    public async Task<bool> SendPasswordResetEmailAsync(string email, string userName, string resetToken)
    {
        var resetUrl = $"{_configuration["App:Url"]}/account/reset-password?token={resetToken}";
        
        var body = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background: linear-gradient(135deg, #d32f2f, #f57c00); color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 20px; background: #f9f9f9; }}
                    .button {{ display: inline-block; padding: 12px 24px; background: #d32f2f; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
                    .warning {{ background: #fff3e0; border-left: 4px solid #f57c00; padding: 10px; margin: 10px 0; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Επαναφορά Κωδικού</h1>
                    </div>
                    <div class='content'>
                        <h2>Αλλαγή Κωδικού Πρόσβασης</h2>
                        <p>Αγαπητέ/ή {userName},</p>
                        <p>Λάβαμε αίτημα για επαναφορά του κωδικού πρόσβασής σας. Κάντε κλικ στο παρακάτω κουμπί για να ορίσετε νέο κωδικό:</p>
                        <p style='text-align: center;'>
                            <a href='{resetUrl}' class='button'>Επαναφορά Κωδικού</a>
                        </p>
                        <div class='warning'>
                            <strong>Προειδοποίηση:</strong> Ο σύνδεσμος θα λήξει σε 30 λεπτά. Αν δεν ζητήσατε επαναφορά κωδικού, παρακαλώ αγνοήστε αυτό το email.
                        </div>
                    </div>
                </div>
            </body>
            </html>";

        return await SendEmailAsync(email, "Επαναφορά Κωδικού - INCIS SaaP", body);
    }

    /// <summary>
    /// Αποστολή email 2FA
    /// </summary>
    public async Task<bool> SendTwoFactorCodeEmailAsync(string email, string userName, string code)
    {
        var body = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                    .code {{ font-size: 32px; font-weight: bold; letter-spacing: 8px; text-align: center; padding: 20px; background: #f5f5f5; border-radius: 8px; }}
                </style>
            </head>
            <body>
                <h2>Κωδικός Επαλήθευσης</h2>
                <p>Αγαπητέ/ή {userName},</p>
                <p>Ο κωδικός επαλήθευσης two-factor είναι:</p>
                <div class='code'>{code}</div>
                <p>Ο κωδικός θα λήξει σε 5 λεπτά.</p>
            </body>
            </html>";

        return await SendEmailAsync(email, "Κωδικός 2FA - INCIS SaaP", body);
    }

    /// <summary>
    /// Αποστολή email ειδοποίησης νέας σύνδεσης
    /// </summary>
    public async Task<bool> SendNewLoginNotificationAsync(string email, string userName, string ipAddress, string location)
    {
        var body = $@"
            <h2>Νέα σύνδεση στο λογαριασμό σας</h2>
            <p>Αγαπητέ/ή {userName},</p>
            <p>Μια νέα σύνδεση πραγματοποιήθηκε στο λογαριασμό σας:</p>
            <ul>
                <li><strong>Διεύθυνση IP:</strong> {ipAddress}</li>
                <li><strong>Τοποθεσία:</strong> {location}</li>
                <li><strong>Χρόνος:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</li>
            </ul>
            <p>Αν δεν ήταν εσείς, παρακαλώ αλλάξτε τον κωδικό σας αμέσως.</p>";

        return await SendEmailAsync(email, "Νέα Σύνδεση - INCIS SaaP", body);
    }
}
