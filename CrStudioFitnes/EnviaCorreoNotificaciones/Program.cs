using EnviaCorreoNotificaciones;
using EnviaCorreoNotificaciones.Data;
using EnviaCorreoNotificaciones.Services;
using EnviaCorreoNotificaciones.Workers;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Logger global
Log.Logger = new LoggerConfiguration()
    .WriteTo.File(@"C:\Servicios\EnviaNotificaciones\logs\worker-.log",
                  rollingInterval: RollingInterval.Day)
    .WriteTo.EventLog("EnviaCorreoNotificaciones", manageEventSource: true)
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    // Windows Service
    builder.Services.AddWindowsService(o => o.ServiceName = "CRStudio.EmailWorker");

    // DB
    builder.Services.AddDbContext<WorkerDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Settings
    builder.Services.Configure<SchedulerSettings>(builder.Configuration.GetSection("Scheduler"));

    // ? Serilog en HostApplicationBuilder (NO existe builder.Host.UseSerilog)
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog(Log.Logger, dispose: true);

    // ? Evitar duplicados: dejá UN solo registro del sender (recomiendo Singleton por el cache)
    builder.Services.AddSingleton<IEmailConfigProvider, EmailConfigProvider>();
    builder.Services.AddSingleton<IEmailSender, DbEmailSender>();

    // Workers
    builder.Services.AddHostedService<Worker>();
    builder.Services.AddHostedService<ReservationReminderWorker>();

    var host = builder.Build();

    // (opcional) asegurar flush al cerrar
    host.Services.GetRequiredService<IHostApplicationLifetime>()
        .ApplicationStopped.Register(Log.CloseAndFlush);

    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "El servicio falló al iniciar");
    Log.CloseAndFlush();
    throw;
}
