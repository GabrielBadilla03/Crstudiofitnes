using EnviaCorreoNotificaciones.Data;
using EnviaCorreoNotificaciones.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EnviaCorreoNotificaciones.Services
{
    public class EmailConfigProvider : IEmailConfigProvider
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private EmailConfiguracion? _cached;
        private DateTime _cachedAtUtc;
        private readonly TimeSpan _cacheFor = TimeSpan.FromMinutes(5);

        private const string Tipo = "Notificaciones";

        public EmailConfigProvider(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<EmailConfiguracion> GetNotificacionesAsync(CancellationToken ct)
        {
            if (_cached != null && (DateTime.UtcNow - _cachedAtUtc) < _cacheFor)
                return _cached;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();

            var cfg = await db.EmailConfiguraciones.AsNoTracking()
                .Where(x => x.Activo && x.Tipo == Tipo)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(ct);

            if (cfg == null)
                throw new InvalidOperationException($"No existe EmailConfiguracion activa para Tipo='{Tipo}'.");

            // Por si guardaron la app password con espacios
            cfg.Password = (cfg.Password ?? "").Replace(" ", "").Trim();

            _cached = cfg;
            _cachedAtUtc = DateTime.UtcNow;
            return cfg;
        }
    }
}
