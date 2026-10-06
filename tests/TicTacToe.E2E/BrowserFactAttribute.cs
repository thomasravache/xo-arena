using Xunit;

namespace TicTacToe.E2E;

/// <summary>
/// Executa testes de navegador somente se a variável de ambiente E2E_BROWSER for "1".
/// Caso contrário, pula a execução exibindo a instrução correspondente.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class BrowserFactAttribute : FactAttribute
{
    public const string SkipReason = "Defina E2E_BROWSER=1 para rodar os testes de navegador";

    public BrowserFactAttribute()
        : this(Environment.GetEnvironmentVariable("E2E_BROWSER"))
    {
    }

    internal BrowserFactAttribute(string? envValue)
    {
        if (envValue != "1")
        {
            Skip = SkipReason;
        }
    }
}
