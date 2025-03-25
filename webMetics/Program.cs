using Microsoft.AspNetCore.HttpOverrides;
using Duende.IdentityServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.Cookies; // Add this using statement
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;

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

            builder.Services.AddHttpContextAccessor();

            // Add authentication services
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // Set default scheme
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>  // Add this
            {
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(20); //configure the cookie expiration
                options.SlidingExpiration = true;
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = "https://localhost:5001"; // Use the default Duende IdentityServer URL for testing
                options.ClientId = "client";       // And these to match your client configuration
                options.ClientSecret = "secret";   // IMPORTANT: Store securely in production
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.RequireHttpsMetadata = false;  // Only for development! Use HTTPS in production.
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("api1"); // Add the scope for your API
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });

            // Add IdentityServer services
            var identityServerBuilder = builder.Services.AddIdentityServer()
                .AddInMemoryClients(Config.Clients)
                .AddInMemoryIdentityResources(Config.IdentityResources)
                .AddInMemoryApiScopes(Config.ApiScopes)
                .AddInMemoryApiResources(Config.ApiResources)
                .AddTestUsers(Config.GetUsers()); // Use test users
            
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
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
            app.UseIdentityServer(); // Add IdentityServer to the pipeline. Make sure it's before UseAuthentication and UseAuthorization
            app.UseAuthentication();  //Make sure this is called.
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }

    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile()
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
            {
                new ApiScope("api1", "My API")
            };

        public static IEnumerable<ApiResource> ApiResources =>
            new ApiResource[]
            {
                 new ApiResource("api", "My API", new[] { "role" }) // "role" claim
                {
                    Scopes = { "api1" }
                }
            };

        public static IEnumerable<Client> Clients =>
            new Client[]
            {
                new Client
                {
                    ClientId = "client",
                    AllowedGrantTypes = GrantTypes.Code,
                    RedirectUris = { "https://localhost:5002/signin-oidc" }, //  Use a port that you will use for your client application.
                    PostLogoutRedirectUris = { "https://localhost:5002/signout-callback-oidc" },  //  Use a port that you will use for your client application.
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "api1"
                    },
                    ClientSecrets = { new Secret("secret".Sha256()) },
                    RequirePkce = true
                }
            };

        public static List<TestUser> GetUsers()
        {
            return new List<TestUser>
            {
                new TestUser
                {
                    SubjectId = "docencia.metics2@ucr.ac.cr",
                    Username = "docencia.metics2",
                    Password = "12345",
                    Claims = new List<System.Security.Claims.Claim>
                    {
                        new System.Security.Claims.Claim("name", "Alice"),
                        new System.Security.Claims.Claim("role", "0"),
                    }
                },
                new TestUser
                {
                    SubjectId = "docencia.metics3@ucr.ac.cr",
                    Username = "docencia.metics3",
                    Password = "12345",
                    Claims = new List<System.Security.Claims.Claim>
                    {
                        new System.Security.Claims.Claim("name", "Alice"),
                        new System.Security.Claims.Claim("role", "2"),
                    }
                },
                new TestUser
                {
                    SubjectId = "docencia.metics1@ucr.ac.cr",
                    Username = "docencia.metics1",
                    Password = "12345",
                    Claims = new List<System.Security.Claims.Claim>
                    {
                        new System.Security.Claims.Claim("name", "Alice"),
                        new System.Security.Claims.Claim("role", "1"),
                    }
                },
            };
        }
    }
}