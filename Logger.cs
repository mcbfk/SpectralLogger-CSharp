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
                Console.WriteLine("Inicializando Logger...");

                // Define o caminho do arquivo de log na raiz do projeto
                var projectDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
                var logsDirectory = Path.Combine(projectDirectory, "logs");
                _logFilePath = Path.Combine(logsDirectory, "application.log");
                Console.WriteLine($"Caminho do arquivo de log: {_logFilePath}");

                // Cria o diretório de logs se não existir
                if (!Directory.Exists(logsDirectory))
                {
                    Console.WriteLine("Criando diretório de logs...");
                    Directory.CreateDirectory(logsDirectory);
                    Console.WriteLine($"Diretório criado: {logsDirectory}");
                }

                // Inicializa o canal e o token de cancelamento
                _logChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
                _cts = new CancellationTokenSource();

                // Tenta criar/abrir o arquivo de log
                Console.WriteLine("Abrindo arquivo de log...");
                try
                {
                    _logFileWriter = new StreamWriter(_logFilePath, true) { AutoFlush = true };
                    Console.WriteLine("StreamWriter criado com sucesso");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERRO ao criar StreamWriter: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    throw;
                }

                // Verifica se o arquivo foi criado
                if (File.Exists(_logFilePath))
                {
                    Console.WriteLine($"Arquivo de log criado com sucesso em: {_logFilePath}");
                    Console.WriteLine($"Tamanho do arquivo: {new FileInfo(_logFilePath).Length} bytes");
                    _isInitialized = true;
                }
                else
                {
                    Console.WriteLine($"AVISO: Arquivo de log não foi criado em: {_logFilePath}");
                    Console.WriteLine("Tentando criar arquivo manualmente...");
                    try
                    {
                        File.WriteAllText(_logFilePath, "=== Início do arquivo de log ===\n");
                        Console.WriteLine("Arquivo criado manualmente com sucesso");
                        _isInitialized = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERRO ao criar arquivo manualmente: {ex.Message}");
                        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                        throw new FileNotFoundException($"Arquivo de log não foi criado em: {_logFilePath}", ex);
                    }
                }

                // Inicia o processamento de logs
                Console.WriteLine("Iniciando processamento de logs...");
                _ = ProcessLogQueueAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO FATAL ao inicializar o logger:");
                Console.WriteLine($"Mensagem: {ex.Message}");
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
                Console.WriteLine("AVISO: Logger não inicializado corretamente");
                return;
            }

            if (level < _currentLogLevel) return;

            try
            {
                string logEntry = FormatLogEntry(level, message, ex);
                if (!_logChannel.Writer.TryWrite(logEntry))
                {
                    Console.WriteLine("AVISO: Não foi possível escrever no canal de logs");
                }
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"Erro ao escrever log: {logEx.Message}");
                Console.WriteLine($"Stack Trace: {logEx.StackTrace}");
            }
        }

        public async Task LogAsync(LogLevel level, string message, Exception? ex = null)
        {
            if (!_isInitialized)
            {
                Console.WriteLine("AVISO: Logger não inicializado corretamente");
                return;
            }

            try
            {
                string logEntry = FormatLogEntry(level, message, ex);
                await _logChannel.Writer.WriteAsync(logEntry);
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"Erro ao escrever log assíncrono: {logEx.Message}");
                Console.WriteLine($"Stack Trace: {logEx.StackTrace}");
            }
        }

        private string FormatLogEntry(LogLevel level, string message, Exception? ex)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            if (ex != null)
            {
                logEntry += $"\nExceção: {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}\n";
            }
            return logEntry;
        }

        private async Task ProcessLogQueueAsync(CancellationToken token)
        {
            try
            {
                Console.WriteLine("Processador de logs iniciado");
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
                            Console.WriteLine($"ERRO ao escrever no arquivo: {ex.Message}");
                            Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                            // Tentar reabrir o arquivo
                            try
                            {
                                Console.WriteLine("Tentando reabrir o arquivo de log...");
                                _logFileWriter.Dispose();
                                _logFileWriter = new StreamWriter(_logFilePath, true) { AutoFlush = true };
                                await _logFileWriter.WriteLineAsync(logEntry);
                                await _logFileWriter.FlushAsync();
                                Console.WriteLine("Arquivo reaberto com sucesso");
                            }
                            catch (Exception reopenEx)
                            {
                                Console.WriteLine($"ERRO ao reabrir arquivo: {reopenEx.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao processar entrada de log: {ex.Message}");
                        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Processamento de logs cancelado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no processamento de logs: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        public async Task FlushAsync()
        {
            if (!_isInitialized)
            {
                Console.WriteLine("AVISO: Logger não inicializado corretamente");
                return;
            }

            try
            {
                Console.WriteLine("Finalizando canal de logs...");
                _logChannel.Writer.Complete();
                await _logChannel.Reader.Completion;

                Console.WriteLine("Salvando logs no arquivo...");
                await _logFileWriter.FlushAsync();

                // Verificar se o arquivo existe e tem conteúdo
                if (File.Exists(_logFilePath))
                {
                    var fileInfo = new FileInfo(_logFilePath);
                    Console.WriteLine($"Arquivo de log finalizado: {fileInfo.Length} bytes");
                }
                else
                {
                    Console.WriteLine("AVISO: Arquivo de log não existe após finalização");
                }

                Console.WriteLine("Logs finalizados com sucesso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao finalizar logs: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        public void Dispose()
        {
            try
            {
                Console.WriteLine("Dispondo recursos do logger...");
                _cts.Cancel();
                _logFileWriter?.Dispose();
                GC.SuppressFinalize(this);
                Console.WriteLine("Logger disposto com sucesso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao dispor logger: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
}