namespace console_app.Services;

/// <summary>
///     Represents a service with a name.
/// </summary>
/// <remarks>This service encapsulates a single name and provides functionality to retrieve it.</remarks>
/// <param name="Name"></param>
internal record MyFirstService(string Name)
{
    public string GetName() => Name;
}