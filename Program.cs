using System;
using System.IO;
using System.Threading.Tasks;
using SpectralLogger;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== LOGGER DIAGNOSTICS ===");
        Console.WriteLine($"Current directory: {Environment.CurrentDirectory}");
        Console.WriteLine($"Application base directory: {AppDomain.CurrentDomain.BaseDirectory}");

        // Check if logs directory exists
        var projectDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
        var logsDirectory = Path.Combine(projectDirectory, "logs");
        Console.WriteLine($"Project directory: {projectDirectory}");
        Console.WriteLine($"Expected logs directory: {logsDirectory}");
        Console.WriteLine($"Logs directory exists: {Directory.Exists(logsDirectory)}");

        // Check if log file exists
        var logFilePath = Path.Combine(logsDirectory, "application.log");
        Console.WriteLine($"Log file path: {logFilePath}");
        Console.WriteLine($"Log file exists: {File.Exists(logFilePath)}");

        // Check permissio
        try
        {
            if (!Directory.Exists(logsDirectory))
            {
                Console.WriteLine("Attempting to create logs directory...");
                Directory.CreateDirectory(logsDirectory);
                Console.WriteLine("Logs directory created successfully");
            }

            // Test write permissions
            var testFile = Path.Combine(logsDirectory, "test.txt");
            File.WriteAllText(testFile, "Permission test");
            File.Delete(testFile);
            Console.WriteLine("Write permissions OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR checking permissions: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }

        Console.WriteLine("\n=== STARTING LOGGER ===");

        using (Logger.Instance)
        {
            try
            {
                Console.WriteLine("Setting log level...");
                Logger.Instance.SetLogLevel(Logger.LogLevel.Debug);

                Console.WriteLine("Testing logs...");
                Logger.Instance.Log(Logger.LogLevel.Info, "Application started!");
                Logger.Instance.Log(Logger.LogLevel.Debug, "Debug log test");
                Logger.Instance.Log(Logger.LogLevel.Warning, "Warning log test");

                try
                {
                    throw new InvalidOperationException("Simulated error for testing");
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log(Logger.LogLevel.Error, "Operation failed", ex);
                }

                Console.WriteLine("Testing async logs...");
                await Logger.Instance.LogAsync(Logger.LogLevel.Warning, "Alert: Memory usage is high");

                Console.WriteLine("Finalizing logs...");
                await Logger.Instance.FlushAsync();

                // Check if file was created
                Console.WriteLine("\n=== FINAL VERIFICATION ===");
                Console.WriteLine($"Log file exists after operations: {File.Exists(logFilePath)}");
                if (File.Exists(logFilePath))
                {
                    var fileInfo = new FileInfo(logFilePath);
                    Console.WriteLine($"File size: {fileInfo.Length} bytes");
                    Console.WriteLine($"Creation time: {fileInfo.CreationTime}");
                    Console.WriteLine($"Last modified: {fileInfo.LastWriteTime}");
                }

                Console.WriteLine("Logs finalized successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}