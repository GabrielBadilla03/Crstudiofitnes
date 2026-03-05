using EnviaCorreoNotificaciones.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnviaCorreoNotificaciones.Services
{
    public interface IEmailConfigProvider
    {
        Task<EmailConfiguracion> GetNotificacionesAsync(CancellationToken ct);
    }
}
