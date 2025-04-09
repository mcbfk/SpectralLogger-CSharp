using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SpectralLogger // cria um container logico e unico 
{
    public sealed class Logger : IDisposable
    {
        public enum LogLevel { Debug, Info, Warning, Error }

        private static readonly Lazy<Logger> _instance = new Lazy<Logger>(() => new Logger());
        public static Logger Instance => _instance.Value;

        private readonly Channel<string> _logChannel;
        private readonly CancellationTokenSource _cts;
        private readonly StreamWriter _logFileWriter;
        private LogLevel _currentLogLevel = LogLevel.Info;

        private static readonly ConsoleColor[] _levelColors =
        {
            ConsoleColor.Gray,    // Debug
            ConsoleColor.White,   // Info
            ConsoleColor.Yellow,  // Warning
            ConsoleColor.Red      // Error
        };

        private Logger()
        {
            _logChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
            _cts = new CancellationTokenSource();
            _logFileWriter = new StreamWriter("application.log") { AutoFlush = true };

            // Inicia o processamento de logs
            _ = ProcessLogQueueAsync(_cts.Token); // Corrigido o nome do método
        }

        public void SetLogLevel(LogLevel level) => _currentLogLevel = level;

        public void Log(LogLevel level, string message, Exception? ex = null) // Adicionado '?' para tipo anulável
        {
            if (level < _currentLogLevel) return;

            string logEntry = FormatLogEntry(level, message, ex);
            _logChannel.Writer.TryWrite(logEntry);
        }

        public async Task LogAsync(LogLevel level, string message, Exception? ex = null)
        {
            string logEntry = FormatLogEntry(level, message, ex);
            await _logChannel.Writer.WriteAsync(logEntry);
        }

        private string FormatLogEntry(LogLevel level, string message, Exception? ex)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            if (ex != null)
            {
                logEntry += $"\nExceção: {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}\n";
            }
            return logEntry; // Garantido retorno em todos os caminhos
        }

        private async Task ProcessLogQueueAsync(CancellationToken token)
        {
            try
            {
                await foreach (var logEntry in _logChannel.Reader.ReadAllAsync(token))
                {
                    var levelStart = logEntry.IndexOf('[') + 1;
                    var levelEnd = logEntry.IndexOf(']');
                    var levelStr = logEntry.Substring(levelStart, levelEnd - levelStart);
                    var level = (LogLevel)Enum.Parse(typeof(LogLevel), levelStr);

                    Console.ForegroundColor = _levelColors[(int)level];
                    Console.WriteLine(logEntry);
                    Console.ResetColor();

                    await _logFileWriter.WriteLineAsync(logEntry);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO NO LOGGER: {ex}");
            }
        }

        public async Task FlushAsync()
        {
            _logChannel.Writer.Complete();
            await _logChannel.Reader.Completion;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _logFileWriter.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}