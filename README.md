# Spectral Solutions - C# Logger

Implementation of a robust logger in C# following the technical test requirements.

## Implemented Features

- ✅ **Singleton Design Pattern**: Ensures a single logger instance throughout the application.
- ✅ **Asynchronous Logging**: Background processing using `System.Threading.Channels`.
- ✅ **Thread Safety**: Safe operations in multi-threaded environments.
- ✅ **Log Levels**: `Debug`, `Info`, `Warning`, `Error` with configurable filtering.
- ✅ **Multiple Destinations**: Console output (with colors) and file writing (`application.log`).
- ✅ **Exception Handling**: Complete stack trace included in error logs.
- ✅ **Resource Management**: `IDisposable` implementation for proper cleanup.

## Requirements

- .NET 6.0 or higher
- Visual Studio 2022 or VS Code with C# extensions

## How to Use

1. Clone the repository:
   ```bash
   git clone https://github.com/mcbfk/SpectralSolutions-Logger.git
   ```

2. Add the project reference to your solution.

3. Usage example:
   ```csharp
   using SpectralLogger;

   // Configure log level
   Logger.Instance.SetLogLevel(Logger.LogLevel.Debug);

   // Synchronous logs
   Logger.Instance.Log(Logger.LogLevel.Info, "Application started");
   Logger.Instance.Log(Logger.LogLevel.Warning, "Memory usage is high");

   // Asynchronous logs
   await Logger.Instance.LogAsync(Logger.LogLevel.Debug, "Asynchronous operation");

   // Logs with exception
   try {
       // your code here
   } catch (Exception ex) {
       Logger.Instance.Log(Logger.LogLevel.Error, "Operation failed", ex);
   }

   // Ensure all logs are processed
   await Logger.Instance.FlushAsync();
   ```

## Project Structure

- `Logger.cs`: Main logger implementation
- `Program.cs`: Usage examples
- `application.log`: Log file generated during execution

## Design Considerations

- Use of `Channel` for thread-safe communication between producers and consumers
- Asynchronous processing for better performance
- Different colors in console for each log level
- Standardized timestamp and message formatting

## Future Improvements

- [ ] Support for multiple log destinations (e.g., database, external services)
- [ ] Configuration via file (appsettings.json)
- [ ] Log file rotation
- [ ] Performance metrics
- [ ] Support for structured logging
