# Spectral Solutions - Logger em C#

Implementação de um logger seguindo os requisitos do teste técnico.

## Funcionalidades Implementadas
- ✅ **Singleton Design Pattern**: Garante uma única instância do logger.
- ✅ **Logs Assíncronos**: Processamento em segundo plano usando `System.Threading.Channels`.
- ✅ **Thread Safety**: Operações seguras em ambientes multi-thread.
- ✅ **Níveis de Log**: `Debug`, `Info`, `Warning`, `Error` com filtragem configurável.
- ✅ **Destinos Múltiplos**: Escrita no console e em arquivo (`application.log`).
- ✅ **Tratamento de Exceções**: Stack trace completo incluso nos logs de erro.

## Como Usar
1. Clone o repositório:
   git clone https://github.com/seu-usuario/SpectralSolutions-Logger.git