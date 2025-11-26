
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using GadgetsOnline.Models;
using GadgetsOnline.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Configuration;
using System.Data.Entity;
using System.Text.Json;
using System.Threading.Tasks;

namespace GadgetsOnline
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            ConfigurationManager.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(1);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            services.AddControllersWithViews();
            
            
            
            
            
            
            /////////Adderd by AES
            ///

            // Add Entity Framework - Always use SQL Server (RDS)
            // Default: Use AWS Secrets Manager
            // Fallback: Use local connection string from configuration
            string connectionString;

            try
            {
                // Primary: Build connection string from AWS Secrets Manager
                connectionString = BuildConnectionStringFromSecretsManager().GetAwaiter().GetResult();
            }
            catch
            {
                // Fallback: Use local connection string if Secrets Manager is unavailable
                var localConnectionString = Configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(localConnectionString))
                {
                    throw new InvalidOperationException("Unable to retrieve connection string from AWS Secrets Manager and no local connection string is configured.");
                }
                connectionString = localConnectionString;
            }

            //services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped<GadgetsOnlineEntities>(provider => new GadgetsOnlineEntities(connectionString));



            ////End of Added by AES



            //services.AddScoped<GadgetsOnlineEntities>(provider => new GadgetsOnlineEntities(Configuration.GetConnectionString(nameof(GadgetsOnlineEntities))));





            Database.SetInitializer(new GadgetsOnlineInitializer());

            services.AddScoped<IInventory, Inventory>();
            services.AddScoped<IShoppingCart, ShoppingCart>();
            services.AddScoped<IOrderProcessing, OrderProcessing>();
            //Added Services
        }


        private static async Task<string> BuildConnectionStringFromSecretsManager()
        {
            var secretsService = new SecretsManagerService();
            string secretJson;

            try
            {
                secretJson = await secretsService.GetSecretAsync("atx-db-modernization-atx-db-modernization-1-target");
                if (!string.IsNullOrWhiteSpace(secretJson))
                {
                    var username = secretsService.GetFieldFromSecret(secretJson, "username");
                    var password = secretsService.GetFieldFromSecret(secretJson, "password");
                    var host = secretsService.GetFieldFromSecret(secretJson, "host");
                    var port = secretsService.GetFieldFromSecret(secretJson, "port");
                    var dbname = "dmg";

                    return $"Host={host};Port={port};Database={dbname};Username={username};Password={password};SslMode=Require;Trust Server Certificate=true";
                }
            }
            catch (Exception)
            {
            }

            secretJson = await secretsService.GetSecretByDescriptionPrefixAsync("Password for RDS MSSQL used for MAM319.");
            if (!string.IsNullOrWhiteSpace(secretJson))
            {
                var username = secretsService.GetFieldFromSecret(secretJson, "username");
                var password = secretsService.GetFieldFromSecret(secretJson, "password");
                var host = secretsService.GetFieldFromSecret(secretJson, "host");
                var port = secretsService.GetFieldFromSecret(secretJson, "port");
                var dbname = secretsService.GetFieldFromSecret(secretJson, "dbname");


                return $"Server={host},{port};Database={dbname};User Id={username};Password={password};TrustServerCertificate=true;Encrypt=true";
            }

            throw new InvalidOperationException("Failed to retrieve database credentials from Secrets Manager.");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Initialize EF6 database on startup
            using (var context = new GadgetsOnlineEntities(BuildConnectionStringFromSecretsManager().GetAwaiter().GetResult()))
            {
                // This will trigger the initializer if needed
                context.Database.Initialize(force: false);
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            //Added Middleware

            app.UseRouting();

            app.UseAuthorization();

            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }

    public class ConfigurationManager
    {
        public static IConfiguration Configuration { get; set; }
    }

    public class SecretsManagerService
    {
        private readonly IAmazonSecretsManager _secretsManager;

        public SecretsManagerService()
        {
            _secretsManager = new AmazonSecretsManagerClient(RegionEndpoint.USEast1);
        }

        public SecretsManagerService(IAmazonSecretsManager secretsManager)
        {
            _secretsManager = secretsManager;
        }

        /// <summary>
        /// Retrieves a secret by its exact name
        /// </summary>
        /// <param name="secretName">The name of the secret</param>
        /// <returns>The secret value as a string</returns>
        public async Task<string> GetSecretAsync(string secretName)
        {
            try
            {
                var request = new GetSecretValueRequest
                {
                    SecretId = secretName
                };

                var response = await _secretsManager.GetSecretValueAsync(request);
                return response.SecretString;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving secret '{secretName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Finds a secret by searching for a description that starts with a given prefix
        /// </summary>
        /// <param name="descriptionPrefix">The prefix to search for in the description</param>
        /// <returns>The secret value as a string</returns>
        public async Task<string> GetSecretByDescriptionPrefixAsync(string descriptionPrefix)
        {
            try
            {
                var listRequest = new ListSecretsRequest();
                var listResponse = await _secretsManager.ListSecretsAsync(listRequest);

                foreach (var secret in listResponse.SecretList)
                {
                    if (!string.IsNullOrEmpty(secret.Description) &&
                        secret.Description.StartsWith(descriptionPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        // Found the secret, now retrieve its value
                        var getRequest = new GetSecretValueRequest
                        {
                            SecretId = secret.ARN
                        };
                        var getResponse = await _secretsManager.GetSecretValueAsync(getRequest);
                        return getResponse.SecretString;
                    }
                }

                throw new Exception($"No secret found with description starting with '{descriptionPrefix}'");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error finding secret by description prefix '{descriptionPrefix}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses a JSON secret string and extracts a specific field
        /// </summary>
        /// <param name="secretJson">The JSON secret string</param>
        /// <param name="fieldName">The field name to extract</param>
        /// <returns>The field value</returns>
        public string GetFieldFromSecret(string secretJson, string fieldName)
        {
            try
            {
                using var document = JsonDocument.Parse(secretJson);
                if (document.RootElement.TryGetProperty(fieldName, out var value))
                {
                    // Handle both string and number types
                    return value.ValueKind switch
                    {
                        JsonValueKind.String => value.GetString() ?? string.Empty,
                        JsonValueKind.Number => value.GetInt32().ToString(),
                        _ => value.ToString()
                    };
                }
                throw new Exception($"Field '{fieldName}' not found in secret");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing secret field '{fieldName}': {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Data model for database credentials from Secrets Manager
    /// </summary>
    public class DatabaseCredentials
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data model for database connection information from Secrets Manager
    /// </summary>
    public class DatabaseConnectionInfo
    {
        public string Host { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public string DbName { get; set; } = string.Empty;
    }
}

