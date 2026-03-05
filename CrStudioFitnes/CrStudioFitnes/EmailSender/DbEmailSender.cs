using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace CrStudioFitnes.EmailSender
{
    public sealed class DbEmailSender : IEmailSender
    {
        private readonly string _cs;

        private EmailCfg? _cached;
        private DateTime _cachedAtUtc;
        private readonly TimeSpan _cacheFor = TimeSpan.FromMinutes(5);

        private const string Tipo = "Notificaciones";

        public DbEmailSender(IConfiguration config)
            => _cs = config.GetConnectionString("DefaultConnection")
                   ?? throw new InvalidOperationException("DefaultConnection not found.");

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var cfg = await GetNotificacionesAsync();

            using var msg = new MailMessage
            {
                From = new MailAddress(cfg.FromEmail, cfg.FromName ?? "", Encoding.UTF8),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8
            };
            msg.To.Add(email);

            using var client = new SmtpClient(cfg.Host, cfg.Port)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(cfg.Username, cfg.Password),
                EnableSsl = cfg.UseStartTls || cfg.UseSsl,
                Timeout = Math.Max(5, cfg.TimeoutSeconds) * 1000,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            await client.SendMailAsync(msg);
        }

        private async Task<EmailCfg> GetNotificacionesAsync()
        {
            if (_cached != null && (DateTime.UtcNow - _cachedAtUtc) < _cacheFor)
                return _cached;

            const string sql = @"
            SELECT TOP (1)
                   [Host],[Port],[UseSsl],[UseStartTls],
                   [FromEmail],[FromName],[Username],[Password],
                   ISNULL([TimeoutSeconds], 30) AS [TimeoutSeconds]
            FROM [dbo].[EmailConfiguracion] WITH (NOLOCK)
            WHERE [Activo] = 1 AND [Tipo] = @tipo
            ORDER BY [Id] DESC;";

            using var cn = new SqlConnection(_cs);
            await cn.OpenAsync();

            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@tipo", Tipo);

            using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync())
                throw new InvalidOperationException($"No existe EmailConfiguracion activa para Tipo='{Tipo}'.");

            var pwd = rd.IsDBNull(rd.GetOrdinal("Password")) ? "" : rd.GetString(rd.GetOrdinal("Password"));
            pwd = (pwd ?? "").Replace(" ", "").Trim();

            _cached = new EmailCfg(
                Host: rd.GetString(rd.GetOrdinal("Host")),
                Port: rd.GetInt32(rd.GetOrdinal("Port")),
                UseSsl: rd.GetBoolean(rd.GetOrdinal("UseSsl")),
                UseStartTls: rd.GetBoolean(rd.GetOrdinal("UseStartTls")),
                FromEmail: rd.GetString(rd.GetOrdinal("FromEmail")),
                FromName: rd.IsDBNull(rd.GetOrdinal("FromName")) ? "" : rd.GetString(rd.GetOrdinal("FromName")),
                Username: rd.IsDBNull(rd.GetOrdinal("Username")) ? "" : rd.GetString(rd.GetOrdinal("Username")),
                Password: pwd,
                TimeoutSeconds: rd.GetInt32(rd.GetOrdinal("TimeoutSeconds"))
            );

            _cachedAtUtc = DateTime.UtcNow;
            return _cached;
        }

        private sealed record EmailCfg(
            string Host,
            int Port,
            bool UseSsl,
            bool UseStartTls,
            string FromEmail,
            string FromName,
            string Username,
            string Password,
            int TimeoutSeconds
        );
    }
}
