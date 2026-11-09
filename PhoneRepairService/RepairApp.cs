using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.Extensions.WebEncoders;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using PhoneRepairService.Data;
using PhoneRepairService.Services;

namespace PhoneRepairService;

/// <summary>Сборка веб-приложения: сервисы, аутентификация, MVC, инициализация БД.</summary>
public static class RepairApp
{
    public static WebApplication Create(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(RepairApp).Assembly.GetName().Name
        });

        string connectionString = builder.Configuration.GetConnectionString("RepairDb")
            ?? throw new InvalidOperationException("Не задана строка подключения ConnectionStrings:RepairDb.");
        bool seed = builder.Configuration.GetValue("Database:SeedDemoData", true);

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddDbContext<RepairDbContext>(o => o.UseMySQL(connectionString));
        builder.Services.AddScoped<RepairDb>();
        builder.Services.AddScoped<AdminService>();
        builder.Services.AddScoped<MasterService>();
        builder.Services.AddScoped<ClientService>();
        builder.Services.AddScoped<AuthService>();

        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.Cookie.Name = "PhoneRepair.Auth";
            });
        builder.Services.AddAuthorization();
        builder.Services.Configure<WebEncoderOptions>(o =>
            o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
        builder.Services.AddControllersWithViews(options =>
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
            DbInitializer.Initialize(scope.ServiceProvider.GetRequiredService<RepairDbContext>(), seed);

        var ru = new CultureInfo("ru-RU");
        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(ru),
            SupportedCultures = new[] { ru },
            SupportedUICultures = new[] { ru }
        });

        if (!app.Environment.IsDevelopment())
            app.UseExceptionHandler("/Home/Error");

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        return app;
    }
}
