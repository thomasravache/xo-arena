using System.Diagnostics;

namespace TicTacToe.E2E;

/// <summary>
/// Valida se o daemon do Docker está em execução e respondendo.
/// Lança InvalidOperationException com mensagem instruindo a iniciar o Docker caso indisponível.
/// </summary>
public static class DockerValidator
{
    public const string DockerNotAvailableMessage = "Inicie o Docker para executar os testes de navegador.";

    public static async Task EnsureDockerAvailableAsync(
        Func<CancellationToken, Task<bool>>? checkFunc = null,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(60);
        using var cts = new CancellationTokenSource(effectiveTimeout);

        try
        {
            var isAvailable = checkFunc != null
                ? await checkFunc(cts.Token)
                : await CheckDockerDefaultAsync(cts.Token);

            if (!isAvailable)
            {
                throw new InvalidOperationException(DockerNotAvailableMessage);
            }
        }
        catch (OperationCanceledException)
        {
            throw new InvalidOperationException($"{DockerNotAvailableMessage} (Timeout de {effectiveTimeout.TotalSeconds:F0}s atingido)");
        }
    }

    private static async Task<bool> CheckDockerDefaultAsync(CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
