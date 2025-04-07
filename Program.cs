using System;
using System.Threading.Tasks;
using SpectralLogger;

class Program
{
    static async Task Main()
    {
        using (Logger.Instance)
        {
            try
            {
                Logger.Instance.SetLogLevel(Logger.LogLevel.Debug);
                Logger.Instance.Log(Logger.LogLevel.Info, "Aplicação iniciada!");

                try
                {
                    throw new InvalidOperationException("Erro simulado para teste");
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log(Logger.LogLevel.Error, "Falha na operação", ex);
                }

                await Logger.Instance.LogAsync(Logger.LogLevel.Warning, "Alerta: Memória está alta");
                await Logger.Instance.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro fatal: {ex}");
            }
        }

        Console.WriteLine("Pressione qualquer tecla para sair...");
        Console.ReadKey();
    }
}