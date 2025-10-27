using System;
using flibuget.Core.InfraServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using flibuget.ViewModels;

namespace flibuget;

public static class CompositionRoot
{
    public static ServiceProvider ConfigureServices()
    {
        // Determine environment (default to Production if not set)
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Production";

#if DEBUG
        // In Debug builds, default to Development if no environment variable is set
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"))
        && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
        {
            environment = "Development";
        }
#endif

        // Build configuration
        var configuration = new ConfigurationBuilder()
      .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
 .Build();

        // Configure Serilog from appsettings.json
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
      .CreateLogger();

        var services = new ServiceCollection();

        // Register configuration
        services.AddSingleton<IConfiguration>(configuration);

        // Add Serilog logging
        services.AddLogging(builder =>
  {
      builder.ClearProviders();
      builder.AddSerilog(dispose: true);
  });

        // Register ViewModels
        services.AddTransient<MainViewModel>();

        // Register Infrastructure Services here as they are created
        services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHttpClient();
        services.AddTransient<IWebService, HttpService>();

        // Register Domain Services and Repos here as they are created
        //services.AddScoped<IUserDataRepository, UserDataRepository>();

        // Register Application Services here as they are created
        //services.AddScoped<IAppService, AppService>();


        //usage example:
        //var myService = App.ServiceProvider.GetRequiredService<IMyService>();

        Log.Information("Application services configured successfully for environment: {Environment}", environment);

        return services.BuildServiceProvider();
    }
}
