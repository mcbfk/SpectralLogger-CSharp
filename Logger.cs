using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SpectralLogger
{
    public sealed class Logger : IDisposable
    {
        public enum LogLevel { Debug, Info, Warning, Error }

        private static readonly Lazy<Logger> _instance = new Lazy<Logger>(() => new Logger());
        public static Logger Instance => _instance.Value;

        private readonly Channel<string> _logChannel;
        private readonly CancellationTokenSource _cts;
        private StreamWriter _logFileWriter;
        private LogLevel _currentLogLevel = LogLevel.Info;
        private readonly string _logFilePath;
        private bool _isInitialized = false;

        private static readonly ConsoleColor[] _levelColors =
        {
            ConsoleColor.Gray,    // Debug
            ConsoleColor.White,   // Info
            ConsoleColor.Yellow,  // Warning
            ConsoleColor.Red      // Error
        };

        private Logger()
        {
            try
            {
                Console.WriteLine("Initializing Logger...");

                // Define the log file path in the project root
                var projectDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
                var logsDirectory = Path.Combine(projectDirectory, "logs");
                _logFilePath = Path.Combine(logsDirectory, "application.log");
                Console.WriteLine($"Log file path: {_logFilePath}");

                // Create logs directory if it doesn't exist
                if (!Directory.Exists(logsDirectory))
                {
                    Console.WriteLine("Creating logs directory...");
                    Directory.CreateDirectory(logsDirectory);
                    Console.WriteLine($"Directory created: {logsDirectory}");
                }

                // Initialize channel and cancellation token
                _logChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
                _cts = new CancellationTokenSource();

                // Try to create/open the log file
                Console.WriteLine("Opening log file...");
                try
                {
                    _logFileWriter = new StreamWriter(_logFilePath, true) { AutoFlush = true };
                    Console.WriteLine("StreamWriter created successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR creating StreamWriter: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    throw;
                }

                // Verify if the file was created
                if (File.Exists(_logFilePath))
                {
                    Console.WriteLine($"Log file created successfully at: {_logFilePath}");
                    Console.WriteLine($"File size: {new FileInfo(_logFilePath).Length} bytes");
                    _isInitialized = true;
                }
                else
                {
                    Console.WriteLine($"WARNING: Log file was not created at: {_logFilePath}");
                    Console.WriteLine("Trying to create file manually...");
                    try
                    {
                        File.WriteAllText(_logFilePath, "=== Log file start ===\n");
                        Console.WriteLine("File created manually successfully");
                        _isInitialized = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR creating file manually: {ex.Message}");
                        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                        throw new FileNotFoundException($"Log file was not created at: {_logFilePath}", ex);
                    }
                }

                // Start log processing
                Console.WriteLine("Starting log processing...");
                _ = ProcessLogQueueAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL ERROR initializing logger:");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public void SetLogLevel(LogLevel level) => _currentLogLevel = level;

        public void Log(LogLevel level, string message, Exception? ex = null)
        {
            if (!_isInitialized)
            {
                Console.WriteLine("WARNING: Logger not properly initialized");
                return;
            }

            if (level < _currentLogLevel) return;

            try
            {
                string logEntry = FormatLogEntry(level, message, ex);
                if (!_logChannel.Writer.TryWrite(logEntry))
                {
                    Console.WriteLine("WARNING: Could not write to log channel");
                }
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"Error writing log: {logEx.Message}");
                Console.WriteLine($"Stack Trace: {logEx.StackTrace}");
            }
        }

        public async Task LogAsync(LogLevel level, string message, Exception? ex = null)
        {
            if (!_isInitialized)
            {
                Console.WriteLine("WARNING: Logger not properly initialized");
                return;
            }

            try
            {
                string logEntry = FormatLogEntry(level, message, ex);
                await _logChannel.Writer.WriteAsync(logEntry);
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"Error writing async log: {logEx.Message}");
                Console.WriteLine($"Stack Trace: {logEx.StackTrace}");
            }
        }

        private string FormatLogEntry(LogLevel level, string message, Exception? ex)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            if (ex != null)
            {
                logEntry += $"\nException: {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}\n";
            }
            return logEntry;
        }

        private async Task ProcessLogQueueAsync(CancellationToken token)
        {
            try
            {
                Console.WriteLine("Log processor started");
                await foreach (var logEntry in _logChannel.Reader.ReadAllAsync(token))
                {
                    try
                    {
                        var levelStart = logEntry.IndexOf('[') + 1;
                        var levelEnd = logEntry.IndexOf(']');
                        var levelStr = logEntry.Substring(levelStart, levelEnd - levelStart);
                        var level = (LogLevel)Enum.Parse(typeof(LogLevel), levelStr);

                        Console.ForegroundColor = _levelColors[(int)level];
                        Console.WriteLine(logEntry);
                        Console.ResetColor();

                        try
                        {
                            await _logFileWriter.WriteLineAsync(logEntry);
                            await _logFileWriter.FlushAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"ERROR writing to file: {ex.Message}");
                            Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                            // Try to reopen the file
                            try
                            {
                                Console.WriteLine("Trying to reopen log file...");
                                _logFileWriter.Dispose();
                                _logFileWriter = new StreamWriter(_logFilePath, true) { AutoFlush = true };
                                await _logFileWriter.WriteLineAsync(logEntry);
                                await _logFileWriter.FlushAsync();
                                Console.WriteLine("File reopened successfully");
                            }
                            catch (Exception reopenEx)
                            {
                                Console.WriteLine($"ERROR reopening file: {reopenEx.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing log entry: {ex.Message}");
                        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Log processing canceled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in log processing: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        public async Task FlushAsync()
        {
            if (!_isInitialized)
            {
                Console.WriteLine("WARNING: Logger not properly initialized");
                return;
            }

            try
            {
                Console.WriteLine("Finalizing log channel...");
                _logChannel.Writer.Complete();
                await _logChannel.Reader.Completion;

                Console.WriteLine("Saving logs to file...");
                await _logFileWriter.FlushAsync();

                // Verify if file exists and has content
                if (File.Exists(_logFilePath))
                {
                    var fileInfo = new FileInfo(_logFilePath);
                    Console.WriteLine($"Log file size: {fileInfo.Length} bytes");
                    Console.WriteLine($"Last modified: {fileInfo.LastWriteTime}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during flush: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        public void Dispose()
        {
            try
            {
                Console.WriteLine("Disposing logger...");
                _cts.Cancel();
                _cts.Dispose();
                _logFileWriter?.Dispose();
                Console.WriteLine("Logger disposed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing logger: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
}