using System.Numerics;
using Cocona;
using console_app.Commands;
using console_app.Extensions;

var builder = CoconaApp.CreateBuilder(args, options => { options.EnableShellCompletionSupport = true; });

builder.RegisterServices(builder.Configuration, builder.Environment, builder.Logging, builder.Services);

var app = builder.Build();

app.RegisterCommands();

await app.RunAsync();

/// <summary>
///     Represents the state of a Cocona application host, including the arguments passed to the application.
/// </summary>
/// <remarks>
///     This type encapsulates the arguments provided to the application at runtime. It is immutable and can
///     be used to track or compare the state of the application host during its lifecycle.
/// </remarks>
/// <param name="Arguments">
///     The arguments passed to the application. If no arguments are provided, this will be an empty
///     array.
/// </param>
public record struct CoconaAppHostState(params string[] Arguments)
    : IEqualityOperators<CoconaAppHostState, CoconaAppHostState, bool>
{
    public string[] Arguments { get; } = Arguments ?? [];
}

/// <summary>
///     Gets the current state of the Cocona application host.
/// </summary>
internal partial class Program
{
    public static AsyncLocal<CoconaAppHostState> AppHostState { get; } = new();
}