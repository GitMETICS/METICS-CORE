using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using webMetics.Services.Sso;

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

            // Claves de DataProtection persistidas y con nombre de aplicación fijo: necesario
            // para que las cookies de correlación/nonce de OIDC sobrevivan reinicios y
            // funcionen detrás del proxy/granja de la UCR.
            builder.Services.AddDataProtection()
                .SetApplicationName("METICS")
                .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")));

            // Configuración del SSO de la UCR (sección Authentication:UCR; el secreto va en user-secrets).
            var ssoOptions = builder.Configuration.GetSection("Authentication:UCR").Get<UcrSsoOptions>()
                             ?? new UcrSsoOptions();
            builder.Services.AddSingleton(ssoOptions);
            builder.Services.AddScoped<ISsoUserStore, SsoUserStore>();
            builder.Services.AddScoped<UcrSsoService>();

            var authentication = builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = ssoOptions.Enabled
                    ? "oidc"
                    : CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie();

            // El handler de OIDC solo se registra cuando el SSO está configurado, de modo que
            // la app siga arrancando en entornos sin credenciales de la UCR.
            if (ssoOptions.Enabled)
            {
                authentication.AddOpenIdConnect("oidc", options =>
                {
                    options.Authority = ssoOptions.Authority;
                    options.ClientId = ssoOptions.ClientId;
                    if (!string.IsNullOrWhiteSpace(ssoOptions.ClientSecret))
                    {
                        options.ClientSecret = ssoOptions.ClientSecret;
                    }
                    options.RequireHttpsMetadata = ssoOptions.RequireHttpsMetadata;
                    options.ResponseType = "code";
                    options.UsePkce = true;
                    // Conservar los nombres de claim tal cual los envía el IdP (given_name, family_name,
                    // email, mail, ucrMotherSn…). Sin esto, el handler renombra los claims estándar a
                    // las URIs WS-* heredadas y el mapeo por nombre corto fallaría.
                    options.MapInboundClaims = false;
                    options.CallbackPath = ssoOptions.CallbackPath;
                    options.SignedOutCallbackPath = ssoOptions.SignedOutCallbackPath;
                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.Scope.Clear();
                    foreach (string scope in ssoOptions.Scopes)
                    {
                        options.Scope.Add(scope);
                    }
                });
            }

            builder.Services.AddAuthorization();

            var app = builder.Build();

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

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}
