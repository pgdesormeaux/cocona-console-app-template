using Microsoft.Extensions.Logging;

namespace console_app.Services;

/// <summary>
///     Provides functionality for logging messages using the specified logger.
/// </summary>
/// <remarks>
///     This service is designed to log informational messages. It relies on an
///     <see
///         cref="ILogger{TCategoryName}" />
///     instance to perform the logging. Ensure that a valid logger is provided when
///     creating an instance of this class.
/// </remarks>
/// <param name="logger"></param>
public class MySecondService(ILogger<MySecondService> logger)
{
    public void Hello(string message)
    {
        logger.LogInformation(message);
    }
}