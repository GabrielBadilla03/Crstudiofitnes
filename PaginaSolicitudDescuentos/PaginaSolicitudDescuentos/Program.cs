using Microsoft.EntityFrameworkCore;
using PaginaSolicitudDescuentos.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ✅ Registrar OracleContext
builder.Services.AddDbContext<OracleContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleDb"))
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=XXORA_CUSTOMER_MASTER}/{action=Index}/{id?}");

app.Run();
