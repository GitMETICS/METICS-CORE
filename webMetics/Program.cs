using Microsoft.AspNetCore.HttpOverrides;
using Duende.IdentityServer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;

namespace webMetics
{
    public class Program
    {
        //    public static async Task Main(string[] args)
        //    {
        //        var builder = WebApplication.CreateBuilder(args);

        //        builder.Services.AddSingleton<EmailService>();
        //        builder.Services.AddControllersWithViews();
        //        builder.Services.AddRazorPages();
        //        builder.Services.AddDataProtection();

        //        builder.Services.AddDistributedMemoryCache(); // Almacenamiento en memoria para sesiones
        //        builder.Services.AddSession(options =>
        //        {
        //            options.IdleTimeout = TimeSpan.FromMinutes(20); // Tiempo de inactividad de la sesión
        //            options.Cookie.HttpOnly = true; // Solo accesible por HTTP
        //            options.Cookie.IsEssential = true; // Permite el funcionamiento de la sesion aunque el usuario no permita cookies de seguimiento.
        //            options.Cookie.SameSite = SameSiteMode.Lax;
        //            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        //        });

        //        builder.Services.AddHttpContextAccessor();

        //        // Add authentication services
        //        builder.Services.AddAuthentication(options =>
        //        {
        //            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        //            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        //        })
        //        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        //        {
        //            options.Cookie.SameSite = SameSiteMode.Lax;
        //            options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        //            options.SlidingExpiration = true;
        //            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        //        });
        //        //.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        //        //{
        //        //    options.Authority = "https://localhost:5001";
        //        //    options.ClientId = "client";
        //        //    options.ClientSecret = "secret";
        //        //    options.ResponseType = "code";
        //        //    options.SaveTokens = true;
        //        //    options.RequireHttpsMetadata = false;
        //        //    options.Scope.Add("openid");
        //        //    options.Scope.Add("profile");
        //        //    options.Scope.Add("api1");
        //        //    options.TokenValidationParameters = new TokenValidationParameters
        //        //    {
        //        //        NameClaimType = "name",
        //        //        RoleClaimType = "role"
        //        //    };
        //        //    options.CallbackPath = "/signin-oidc"; // Ensure this matches the redirect_uri
        //        //    options.SignedOutCallbackPath = "/signout-callback-oidc";// Ensure this matches the PostLogoutRedirectUris
        //        //    options.Events.OnRemoteFailure = context =>
        //        //    {
        //        //        context.Response.Redirect("/Usuario/IniciarSesion"); // Redirigir a una página de error personalizada
        //        //        context.HandleResponse();
        //        //        return Task.CompletedTask;
        //        //    };
        //        //    options.Events.OnTicketReceived = context =>
        //        //    {
        //        //        // Una vez que el SSO es exitoso y el ticket está listo para crear la cookie local
        //        //        var ucrIdClaim = context.Principal.FindFirst("uid")?.Value;

        //        //        // Aquí se debe llamar a la bitácora para insertar el acceso
        //        //        // var accesoService = context.HttpContext.RequestServices.GetRequiredService<AccesoAUsuarioService>();
        //        //        // accesoService.InsertarAccesoUsuarioBitacora(ucrIdClaim, "EXITO");

        //        //        return Task.CompletedTask;
        //        //    };
        //        //});


        //        // Add IdentityServer services
        //        //builder.Services.AddIdentityServer()
        //        //    .AddInMemoryClients(Config.Clients)
        //        //    .AddInMemoryIdentityResources(Config.IdentityResources)
        //        //    .AddInMemoryApiScopes(Config.ApiScopes)
        //        //    .AddTestUsers(Config.GetUsers());

        //        var app = builder.Build();

        //        // Configure the HTTP request pipeline.
        //        if (!app.Environment.IsDevelopment())
        //        {
        //            app.UseExceptionHandler("/Usuario/IniciarSesion");
        //            app.UseHsts();
        //        }

        //        app.UseForwardedHeaders(new ForwardedHeadersOptions
        //        {
        //            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        //        });

        //        app.UseHttpsRedirection();
        //        app.UseStaticFiles();

        //        app.UseRouting();

        //        app.UseSession();

        //        // app.UseIdentityServer();
        //        app.UseAuthentication();
        //        app.UseAuthorization();

        //        app.MapControllerRoute(
        //            name: "default",
        //            pattern: "{controller=Usuario}/{action=IniciarSesion}/{id?}");
        //        app.MapControllers();
        //        app.MapRazorPages();

        //        app.Run();
        //    }

        // Main without Open Id Connect
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<EmailService>();

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddDataProtection();

            builder.Services.AddDistributedMemoryCache(); // Required to store session data in-memory
            builder.Services.AddSession(options =>
            {
                // Optional: Configure session options
                options.IdleTimeout = TimeSpan.FromMinutes(20); // Set a timeout
                options.Cookie.HttpOnly = true; // Make the session cookie inaccessible to client-side script
                options.Cookie.IsEssential = true; // Mark the cookie as essential
            });

            //builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            //.AddCookie(options =>
            //{
            //    options.LoginPath = "/Usuario/IniciarSesion";
            //});

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            //app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }

    //public static class Config
    //{
    //    public static IEnumerable<IdentityResource> IdentityResources =>
    //        new IdentityResource[]
    //        {
    //            new IdentityResources.OpenId(),
    //            new IdentityResources.Profile()
    //        };

    //    public static IEnumerable<ApiScope> ApiScopes =>
    //        new ApiScope[]
    //        {
    //            new ApiScope("api1", "My API")
    //        };

    //    public static IEnumerable<ApiResource> ApiResources =>
    //        new ApiResource[]
    //        {
    //             new ApiResource("api", "My API", new[] { "role" }) // "role" claim
    //            {
    //                Scopes = { "api1" }
    //            }
    //        };

    //    public static IEnumerable<Client> Clients =>
    //        new Client[]
    //        {
    //            new Client
    //            {
    //                ClientId = "client",
    //                AllowedGrantTypes = GrantTypes.Code,
    //                RedirectUris = { "https://localhost:5002/signin-oidc" },
    //                PostLogoutRedirectUris = { "https://localhost:5002/signout-callback-oidc" },
    //                AllowedScopes =
    //                {
    //                    IdentityServerConstants.StandardScopes.OpenId,
    //                    IdentityServerConstants.StandardScopes.Profile,
    //                    "api1"
    //                },
    //                ClientSecrets = { new Secret("secret".Sha256()) },
    //                RequirePkce = true
    //            }
    //        };

    //    public static List<TestUser> GetUsers()
    //    {
    //        return new List<TestUser>
    //        {
    //            new TestUser
    //            {
    //                SubjectId = "admin.docencia.metics@ucr.ac.cr",
    //                Username = "admin.docencia.metics",
    //                Password = "12345",
    //                Claims = new List<System.Security.Claims.Claim>
    //                {
    //                    new System.Security.Claims.Claim("name", "Alice"),
    //                    new System.Security.Claims.Claim("role", "1"),
    //                }
    //            },
    //            new TestUser
    //            {
    //                SubjectId = "docencia.metics2@ucr.ac.cr",
    //                Username = "docencia.metics2",
    //                Password = "12345",
    //                Claims = new List<System.Security.Claims.Claim>
    //                {
    //                    new System.Security.Claims.Claim("name", "Alice"),
    //                    new System.Security.Claims.Claim("role", "2"),
    //                }
    //            },
    //            new TestUser
    //            {
    //                SubjectId = "docencia.metics3@ucr.ac.cr",
    //                Username = "docencia.metics3",
    //                Password = "12345",
    //                Claims = new List<System.Security.Claims.Claim>
    //                {
    //                    new System.Security.Claims.Claim("name", "Alice"),
    //                    new System.Security.Claims.Claim("role", "1"),
    //                }
    //            },
    //        };
    //    }
    //}
}