using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using MailKit.Net.Smtp;
using System.Text;
using System.Threading.Tasks;

namespace EnviaCorreoNotificaciones.Services
{
    public class DbEmailSender : IEmailSender
    {
        private readonly IEmailConfigProvider _cfg;

        public DbEmailSender(IEmailConfigProvider cfg) => _cfg = cfg;

        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
        {
            var c = await _cfg.GetNotificacionesAsync(ct);

            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(c.FromName ?? "", c.FromEmail));
            msg.To.Add(MailboxAddress.Parse(toEmail));
            msg.Subject = subject;
            msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            var security = c.UseSsl ? SecureSocketOptions.SslOnConnect
                : (c.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

            using var client = new SmtpClient { Timeout = c.TimeoutSeconds * 1000 };

            await client.ConnectAsync(c.Host, c.Port, security, ct);
            await client.AuthenticateAsync(c.Username, c.Password, ct);
            await client.SendAsync(msg, ct);
            await client.DisconnectAsync(true, ct);
        }
    }
}
