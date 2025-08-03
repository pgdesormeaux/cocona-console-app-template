using System.Reflection;
using Cocona.Builder;
using console_app.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace console_app.Extensions;

/// <summary>
///     Provides extension methods for configuring and enhancing a <see cref="CoconaAppBuilder" /> instance.
/// </summary>
/// <remarks>
///     These extension methods allow for streamlined configuration of a Cocona application, including adding
///     environment variables, loading configuration from JSON files, integrating user secrets, setting up logging with
///     Serilog, and registering application services.
/// </remarks>
internal static class AppBuilderExtensions
{
    /// <summary>
    ///     Configures the application by adding environment variables and command-line arguments to the configuration.
    /// </summary>
    /// <remarks>
    ///     This method adds environment variables to the application's configuration. If command-line
    ///     arguments are available in the provided <paramref name="asyncLocal" /> host state, they are also added to the
    ///     configuration as command-line arguments.
    /// </remarks>
    /// <param name="app">The <see cref="CoconaAppBuilder" /> instance to configure.</param>
    /// <param name="configurationBuilder">
    ///     The <see cref="IConfigurationBuilder" /> used to build the application's
    ///     configuration.
    /// </param>
    /// <param name="asyncLocal">
    ///     An <see cref="AsyncLocal{T}" /> containing the application's host state, including
    ///     command-line arguments.
    /// </param>
    /// <returns>The configured <see cref="CoconaAppBuilder" /> instance.</returns>
    public static CoconaAppBuilder ProvideConfigurationFromEnvironmentVariables(
        this CoconaAppBuilder app, IConfigurationBuilder configurationBuilder,
        AsyncLocal<CoconaAppHostState> asyncLocal)
    {
        // Add environment variables to configuration
        configurationBuilder.AddEnvironmentVariables();

        var arguments = asyncLocal.Value.Arguments;

        if (arguments?.Length > 0)
            configurationBuilder.AddCommandLine(arguments
                .ToArray()); // environment from command line e.g.: dotnet run --environment "Staging"

        return app;
    }

    /// <summary>
    ///     Configures the application to load configuration settings from JSON files.
    /// </summary>
    /// <remarks>
    ///     This method adds configuration sources to the <paramref name="configurationBuilder" /> by
    ///     loading settings from:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <c>appsettings.json</c>
    ///                 (required)
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <c>appsettings.{EnvironmentName}.json</c> (optional), where
    ///                 <c>{EnvironmentName}</c> is the value of <see cref="IHostEnvironment.EnvironmentName" />.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     The base path for the configuration files is set to the application's base directory.
    /// </remarks>
    /// <param name="app">The <see cref="CoconaAppBuilder" /> instance to configure.</param>
    /// <param name="configurationBuilder">
    ///     The <see cref="IConfigurationBuilder" /> used to build the application's
    ///     configuration.
    /// </param>
    /// <param name="hostEnvironment">
    ///     The <see cref="IHostEnvironment" /> that provides information about the application's
    ///     hosting environment.
    /// </param>
    /// <returns>The <see cref="CoconaAppBuilder" /> instance, allowing for method chaining.</returns>
    public static CoconaAppBuilder ProvideConfigurationFromAppSettingsJsonFiles(
        this CoconaAppBuilder app,
        IConfigurationBuilder configurationBuilder,
        IHostEnvironment hostEnvironment)
    {
        configurationBuilder.SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", false)
            .AddJsonFile($"appsettings.{hostEnvironment.EnvironmentName}.json", true);

        return app;
    }

    /// <summary>
    ///     Adds user secrets to the configuration builder when the application is running in the development environment.
    /// </summary>
    /// <remarks>
    ///     This method adds user secrets to the configuration only if the application is running in the
    ///     development environment. User secrets are loaded from the assembly containing the application's entry
    ///     point.
    /// </remarks>
    /// <param name="app">The <see cref="CoconaAppBuilder" /> instance to configure.</param>
    /// <param name="configurationBuilder">
    ///     The <see cref="IConfigurationBuilder" /> used to build the application's
    ///     configuration.
    /// </param>
    /// <param name="hostEnvironment">The <see cref="IHostEnvironment" /> representing the current hosting environment.</param>
    /// <returns>The <see cref="CoconaAppBuilder" /> instance, allowing for method chaining.</returns>
    public static CoconaAppBuilder AddUserSecretsInDevelopment(this CoconaAppBuilder app,
        IConfigurationBuilder configurationBuilder,
        IHostEnvironment hostEnvironment)
    {
        if (hostEnvironment.IsDevelopment())
            configurationBuilder.AddUserSecrets(Assembly.GetExecutingAssembly(), true, true);

        return app;
    }

    /// <summary>
    ///     Configures a Serilog bootstrap logger for the application and integrates it with the logging system.
    /// </summary>
    /// <remarks>
    ///     This method sets up a Serilog bootstrap logger to enable logging during the application's
    ///     startup phase. It reads configuration settings from the provided <paramref name="configuration" /> and writes log
    ///     output to the console. The configured logger is then integrated into the application's logging system via the
    ///     <paramref name="loggingBuilder" />.
    /// </remarks>
    /// <param name="app">The <see cref="CoconaAppBuilder" /> instance to configure.</param>
    /// <param name="configuration">The application's configuration, used to initialize the Serilog logger.</param>
    /// <param name="loggingBuilder">The logging builder to which the Serilog logger is added.</param>
    /// <returns>The <see cref="CoconaAppBuilder" /> instance, allowing for method chaining.</returns>
    public static CoconaAppBuilder BootstrapSerilogLogger(this CoconaAppBuilder app, IConfiguration configuration,
        ILoggingBuilder loggingBuilder)
    {
        // Create a Serilog bootstrap logger so that we can log configuration errors.
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        loggingBuilder.AddSerilog();

        return app;
    }

    /// <summary>
    ///     /// Registers application services with the provided <see cref="CoconaAppBuilder" /> instance.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="configurationBuilder"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="loggingBuilder"></param>
    /// <param name="services"></param>
    /// <returns></returns>
    public static CoconaAppBuilder RegisterServices(this CoconaAppBuilder app,
        IConfigurationBuilder configurationBuilder,
        IHostEnvironment hostEnvironment,
        ILoggingBuilder loggingBuilder,
        IServiceCollection services)
    {
        app.ProvideConfigurationFromEnvironmentVariables(configurationBuilder, Program.AppHostState)
            .ProvideConfigurationFromAppSettingsJsonFiles(configurationBuilder, hostEnvironment)
            .AddUserSecretsInDevelopment(configurationBuilder, hostEnvironment)
            .BootstrapSerilogLogger(app.Configuration, loggingBuilder);

        // Register services here if needed.
        // For example, you can register a custom service:
        services.AddSingleton(new MyFirstService("Karen"))
            .AddTransient<MySecondService>()
            .AddOptions();

        return app;
    }
}