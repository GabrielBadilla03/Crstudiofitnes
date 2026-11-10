using EnviaCorreoNotificaciones;
using EnviaCorreoNotificaciones.Data;
using EnviaCorreoNotificaciones.Services;
using EnviaCorreoNotificaciones.Workers;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Los logs se guardan junto al ejecutable por defecto.
// CRSTUDIO_LOG_DIR permite cambiar la carpeta sin recompilar.
var logDirectory = Environment.GetEnvironmentVariable("CRSTUDIO_LOG_DIR");
if (string.IsNullOrWhiteSpace(logDirectory))
{
    logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
}

Directory.CreateDirectory(logDirectory);

var logFile = Path.Combine(logDirectory, "worker-.log");

Log.Logger = new LoggerConfiguration()
    .WriteTo.File(
        logFile,
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    // Windows Service
    builder.Services.AddWindowsService(o =>
        o.ServiceName = "CRStudio.EmailWorker");

    // DB
    builder.Services.AddDbContext<WorkerDbContext>(opt =>
        opt.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection")));

    // Settings
    builder.Services.Configure<SchedulerSettings>(
        builder.Configuration.GetSection("Scheduler"));

    builder.Services.Configure<WhatsAppSettings>(
        builder.Configuration.GetSection("WhatsApp"));

    // Serilog
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog(Log.Logger, dispose: true);

    // Services
    builder.Services.AddSingleton<IEmailConfigProvider, EmailConfigProvider>();
    builder.Services.AddSingleton<IEmailSender, DbEmailSender>();
    builder.Services.AddHttpClient<IWhatsAppSender, MetaWhatsAppSender>();

    // Workers
    builder.Services.AddHostedService<Worker>();
    builder.Services.AddHostedService<ReservationReminderWorker>();

    var host = builder.Build();

    host.Services
        .GetRequiredService<IHostApplicationLifetime>()
        .ApplicationStopped
        .Register(Log.CloseAndFlush);

    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "El servicio falló al iniciar");
    Log.CloseAndFlush();
    throw;
}
