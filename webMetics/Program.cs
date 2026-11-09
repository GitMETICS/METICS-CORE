using Microsoft.AspNetCore.HttpOverrides;

namespace webMetics
{
    public class Program 
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<EmailService>();

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddDataProtection();
            builder.Services.AddSingleton<webMetics.Services.MedallasCatalogoService>();

            var app = builder.Build();

            if (builder.Configuration.GetValue("Medallas:SincronizarAlIniciar", true))
            {
                var added = await app.Services.GetRequiredService<webMetics.Services.MedallasCatalogoService>()
                    .SincronizarAsync();
                app.Logger.LogInformation("Catálogo de medallas sincronizado: {Added} imágenes nuevas registradas.", added);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}
