# Spectral Solutions - Logger em C#

Implementação de um logger robusto em C# seguindo os requisitos do teste técnico.

## Funcionalidades Implementadas

- ✅ **Singleton Design Pattern**: Garante uma única instância do logger em toda a aplicação.
- ✅ **Logs Assíncronos**: Processamento em segundo plano usando `System.Threading.Channels`.
- ✅ **Thread Safety**: Operações seguras em ambientes multi-thread.
- ✅ **Níveis de Log**: `Debug`, `Info`, `Warning`, `Error` com filtragem configurável.
- ✅ **Destinos Múltiplos**: Escrita no console (com cores) e em arquivo (`application.log`).
- ✅ **Tratamento de Exceções**: Stack trace completo incluso nos logs de erro.
- ✅ **Gerenciamento de Recursos**: Implementação de `IDisposable` para limpeza adequada.

## Requisitos

- .NET 6.0 ou superior
- Visual Studio 2022 ou VS Code com extensões C#

## Como Usar

1. Clone o repositório:
   ```bash
   git clone https://github.com/mcbfk/SpectralSolutions-Logger.git
   ```

2. Adicione a referência ao projeto em sua solução.

3. Exemplo de uso:
   ```csharp
   using SpectralLogger;

   // Configurar nível de log
   Logger.Instance.SetLogLevel(Logger.LogLevel.Debug);

   // Logs síncronos
   Logger.Instance.Log(Logger.LogLevel.Info, "Aplicação iniciada");
   Logger.Instance.Log(Logger.LogLevel.Warning, "Memória está alta");

   // Logs assíncronos
   await Logger.Instance.LogAsync(Logger.LogLevel.Debug, "Operação assíncrona");

   // Logs com exceção
   try {
       // seu código aqui
   } catch (Exception ex) {
       Logger.Instance.Log(Logger.LogLevel.Error, "Erro na operação", ex);
   }

   // Garantir que todos os logs sejam processados
   await Logger.Instance.FlushAsync();
   ```

## Estrutura do Projeto

- `Logger.cs`: Implementação principal do logger
- `Program.cs`: Exemplos de uso
- `application.log`: Arquivo de log gerado durante a execução

## Considerações de Design

- Uso de `Channel` para comunicação thread-safe entre produtores e consumidores
- Processamento assíncrono para melhor performance
- Cores diferentes no console para cada nível de log
- Formatação padronizada de timestamp e mensagens

## Melhorias Futuras

- [ ] Suporte a múltiplos destinos de log (ex: banco de dados, serviços externos)
- [ ] Configuração via arquivo (appsettings.json)
- [ ] Rotação de arquivos de log
- [ ] Métricas de performance
- [ ] Suporte a structured logging
