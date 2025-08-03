using Cocona;
using Cocona.Filters;
using console_app.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace console_app.Commands;

/// <summary>
///     Provides extension methods for registering application commands in a <see cref="CoconaApp" />.
/// </summary>
/// <remarks>
///     This class contains methods to define and configure commands for a Cocona application, including
///     support for aliases, optional parameters, sub-commands, dependency injection, and command filters. It enables
///     developers to easily add commands with various configurations and behaviors to their application.
/// </remarks>
public static class AppCommands
{
    /// <summary>
    ///     Registers a set of commands to the specified <see cref="CoconaApp" /> instance.
    /// </summary>
    /// <remarks>
    ///     This method adds various commands to the application, demonstrating features such as:
    ///     <list
    ///         type="bullet">
    ///         <item>
    ///             <description>Setting command aliases and descriptions.</description>
    ///         </item>
    ///         <item>
    ///             <description>Handling optional parameters and arguments.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Using
    ///                 cancellation tokens for long-running commands.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Adding hidden
    ///                 commands.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Injecting dependencies into command
    ///                 parameters.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>Defining sub-commands with filters.</description>
    ///         </item>
    ///         <item>
    ///             <description>Applying command filters, both inline and globally.</description>
    ///         </item>
    ///     </list>
    ///     This method
    ///     is intended to showcase the flexibility and extensibility of the Cocona framework for building command-line
    ///     applications.
    /// </remarks>
    /// <param name="app">The <see cref="CoconaApp" /> instance to which the commands will be registered.</param>
    public static void RegisterCommands(this CoconaApp app)
    {
        // Add a command and set its alias.
        app.AddCommand("hello", (string name) => Console.WriteLine($"Hello {name}"))
            .WithDescription("Say hello")
            .WithAliases("hey", "konnichiwa");

        // Add a command with non-mandatory option and argument.
        // If a method parameter is nullable, it will be treated as non-mandatory.
        app.AddCommand("optional-param",
            (int? age, [Argument] string? name) =>
            {
                Console.WriteLine($"Hello {name ?? "Guest"} ({age?.ToString() ?? "-"})!");
            });

        // Add a command and use the context to cancel with Ctrl+C.
        app.AddCommand("long-running", async (CoconaAppContext ctx) =>
        {
            Console.WriteLine("Running...");
            await Task.Delay(TimeSpan.FromSeconds(30), ctx.CancellationToken);
            Console.WriteLine("Done.");
        });

        // Add a hidden command.
        app.AddCommand("secret-command", () => Console.WriteLine(":-)"))
            .WithMetadata(new HiddenAttribute());

        // Add a command and use Dependency Injection for the command parameter.
        app.AddCommand("with-di", (MyFirstService myService) => Console.WriteLine($"Hello {myService.GetName()}"));

        // Add a sub-command.
        app.AddSubCommand("admin", x =>
        {
            x.AddCommand("start-server", () => Console.WriteLine("Starting the server..."));
            x.AddCommand("stop-server", () => Console.WriteLine("Stopping the server..."));
            x.UseFilter(new RequirePrivilege());
            x.AddCommand("delete-server", () => Console.WriteLine("Deleting the server..."));
        });

        // Add a command with command filters.
        app.AddCommand("with-filter", () => Console.WriteLine("Hello Konnichiwa!"))
            .WithFilter(async (ctx, next) =>
            {
                // Inline CommandFilter
                Console.WriteLine("Before");
                try
                {
                    return await next(ctx);
                }
                finally
                {
                    Console.WriteLine("End");
                }
            });

        // Add a command filter and apply it to commands after this call.
        app.UseFilter(new LoggingFilter(app.Services.GetRequiredService<ILogger<LoggingFilter>>()));
        app.AddCommand("with-global-filter", () => Console.WriteLine("Hello Konnichiwa!"));
    }
}

/// <summary>
///     A filter that logs the execution of commands in a Cocona application.
/// </summary>
/// <remarks>
///     This filter logs a message before and after the execution of a command. It is intended to be used for
///     tracking command execution flow and debugging purposes. The log messages include the name of the command being
///     executed.
/// </remarks>
/// <param name="logger"></param>
internal class LoggingFilter(ILogger<LoggingFilter> logger) : CommandFilterAttribute
{
    public override async ValueTask<int> OnCommandExecutionAsync(CoconaCommandExecutingContext ctx,
        CommandExecutionDelegate next)
    {
        logger.LogInformation($"Before {ctx.Command.Name}");
        try
        {
            return await next(ctx);
        }
        finally
        {
            logger.LogInformation($"End {ctx.Command.Name}");
        }
    }
}

/// <summary>
///     Represents a command filter that enforces a privilege check, ensuring the current user has administrative rights.
/// </summary>
/// <remarks>
///     This filter verifies that the current user is either "Administrator" or "root" before allowing the
///     command to execute.  If the privilege check fails, the command execution is terminated with an error.
/// </remarks>
internal class RequirePrivilege : CommandFilterAttribute
{
    public override ValueTask<int> OnCommandExecutionAsync(CoconaCommandExecutingContext ctx,
        CommandExecutionDelegate next)
    {
        if (Environment.UserName != "Administrator" || Environment.UserName != "root")
            throw new CommandExitedException("Error: Permission denied.", 1);
        return next(ctx);
    }
}