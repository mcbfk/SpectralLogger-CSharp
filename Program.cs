using System;
using System.IO;
using System.Threading.Tasks;
using SpectralLogger;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== DIAGNÓSTICO DO LOGGER ===");
        Console.WriteLine($"Diretório atual: {Environment.CurrentDirectory}");
        Console.WriteLine($"Diretório base da aplicação: {AppDomain.CurrentDomain.BaseDirectory}");

        // Verificar se o diretório de logs existe
        var projectDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
        var logsDirectory = Path.Combine(projectDirectory, "logs");
        Console.WriteLine($"Diretório do projeto: {projectDirectory}");
        Console.WriteLine($"Diretório de logs esperado: {logsDirectory}");
        Console.WriteLine($"Diretório de logs existe: {Directory.Exists(logsDirectory)}");

        // Verificar se o arquivo de log existe
        var logFilePath = Path.Combine(logsDirectory, "application.log");
        Console.WriteLine($"Caminho do arquivo de log: {logFilePath}");
        Console.WriteLine($"Arquivo de log existe: {File.Exists(logFilePath)}");

        // Verificar permissões
        try
        {
            if (!Directory.Exists(logsDirectory))
            {
                Console.WriteLine("Tentando criar diretório de logs...");
                Directory.CreateDirectory(logsDirectory);
                Console.WriteLine("Diretório de logs criado com sucesso");
            }

            // Testar permissões de escrita
            var testFile = Path.Combine(logsDirectory, "test.txt");
            File.WriteAllText(testFile, "Teste de permissões");
            File.Delete(testFile);
            Console.WriteLine("Permissões de escrita OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERRO ao verificar permissões: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }

        Console.WriteLine("\n=== INICIANDO LOGGER ===");

        using (Logger.Instance)
        {
            try
            {
                Console.WriteLine("Configurando nível de log...");
                Logger.Instance.SetLogLevel(Logger.LogLevel.Debug);

                Console.WriteLine("Testando logs...");
                Logger.Instance.Log(Logger.LogLevel.Info, "Aplicação iniciada!");
                Logger.Instance.Log(Logger.LogLevel.Debug, "Teste de log de debug");
                Logger.Instance.Log(Logger.LogLevel.Warning, "Teste de log de warning");

                try
                {
                    throw new InvalidOperationException("Erro simulado para teste");
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log(Logger.LogLevel.Error, "Falha na operação", ex);
                }

                Console.WriteLine("Testando logs assíncronos...");
                await Logger.Instance.LogAsync(Logger.LogLevel.Warning, "Alerta: Memória está alta");

                Console.WriteLine("Finalizando logs...");
                await Logger.Instance.FlushAsync();

                // Verificar novamente se o arquivo foi criado
                Console.WriteLine("\n=== VERIFICAÇÃO FINAL ===");
                Console.WriteLine($"Arquivo de log existe após operações: {File.Exists(logFilePath)}");
                if (File.Exists(logFilePath))
                {
                    var fileInfo = new FileInfo(logFilePath);
                    Console.WriteLine($"Tamanho do arquivo: {fileInfo.Length} bytes");
                    Console.WriteLine($"Data de criação: {fileInfo.CreationTime}");
                    Console.WriteLine($"Data de modificação: {fileInfo.LastWriteTime}");
                }

                Console.WriteLine("Logs finalizados com sucesso!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro fatal: {ex}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        Console.WriteLine("\nPressione qualquer tecla para sair...");
        Console.ReadKey();
    }
}