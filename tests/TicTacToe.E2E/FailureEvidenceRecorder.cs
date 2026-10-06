using Microsoft.Playwright;

namespace TicTacToe.E2E;

/// <summary>
/// Grava trace (.zip) e captura de tela (.png) em artifacts/e2e/ quando um teste falha.
/// </summary>
public sealed class FailureEvidenceRecorder
{
    private readonly string _outputDirectory;

    public FailureEvidenceRecorder(string? outputDirectory = null)
    {
        _outputDirectory = outputDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "e2e");
    }

    public string OutputDirectory => _outputDirectory;

    public async Task<(string tracePath, string screenshotPath)> RecordAsync(
        IBrowserContext? context,
        IPage? page,
        string testClassName,
        string testMethodName)
    {
        Directory.CreateDirectory(_outputDirectory);

        var sanitizedClass = SanitizeFileName(testClassName);
        var sanitizedMethod = SanitizeFileName(testMethodName);
        var baseFileName = $"{sanitizedClass}.{sanitizedMethod}";

        var tracePath = Path.Combine(_outputDirectory, $"{baseFileName}.zip");
        var screenshotPath = Path.Combine(_outputDirectory, $"{baseFileName}.png");

        if (page != null)
        {
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = screenshotPath,
                FullPage = true
            });
        }

        if (context != null)
        {
            try
            {
                await context.Tracing.StopAsync(new TracingStopOptions
                {
                    Path = tracePath
                });
            }
            catch (PlaywrightException)
            {
                // Se tracing não estava ativo ou já foi parado, cria o arquivo zip de indicação caso não exista
                if (!File.Exists(tracePath))
                {
                    await File.WriteAllBytesAsync(tracePath, Array.Empty<byte>());
                }
            }
        }

        return (tracePath, screenshotPath);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
