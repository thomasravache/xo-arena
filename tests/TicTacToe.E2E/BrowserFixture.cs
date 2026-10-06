using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Playwright;
using Xunit;

namespace TicTacToe.E2E;

public sealed class BrowserFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly FailureEvidenceRecorder _evidenceRecorder = new();
    private Uri? _baseUri;

    public Uri BaseUri => _baseUri ?? throw new InvalidOperationException("BrowserFixture não foi inicializado.");

    public async Task InitializeAsync()
    {
        // 1. Garante que o Docker está disponível antes de prosseguir
        await DockerValidator.EnsureDockerAvailableAsync();

        // 2. Instala o browser Chromium do Playwright se necessário
        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Falha ao instalar o Chromium do Playwright. Código de saída: {exitCode}");
        }

        // 3. Inicializa Playwright e Chromium
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        // 4. Constrói e inicializa a aplicação pelo AppHost do Aspire
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TicTacToe_AppHost>();
        _app = await appHost.BuildAsync();

        await _app.StartAsync();

        // 5. Espera o webfrontend ficar saudável e obtém a URL HTTP (conforme contrato SPEC-0064)
        Uri endpoint;
        try
        {
            endpoint = _app.GetEndpoint("webfrontend", "http");
        }
        catch
        {
            endpoint = _app.GetEndpoint("webfrontend");
        }

        _baseUri = endpoint;
    }

    public async Task<string> GetSqlConnectionStringAsync()
    {
        if (_app == null)
        {
            throw new InvalidOperationException("BrowserFixture não foi inicializado.");
        }

        var connString = await _app.GetConnectionStringAsync("TicTacToeDb")
            ?? await _app.GetConnectionStringAsync("sqlserver");

        return connString ?? throw new InvalidOperationException("Connection string do SQL Server não encontrada no AppHost.");
    }

    public async Task<PlayerSession> NewPlayerAsync(string name = "Jogador", ViewportSize? viewport = null)
    {
        if (_browser == null || _baseUri == null)
        {
            throw new InvalidOperationException("BrowserFixture não foi inicializado.");
        }

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = viewport ?? new ViewportSize { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });

        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true
        });

        var page = await context.NewPageAsync();
        return new PlayerSession(context, page, _baseUri, name, _evidenceRecorder);
    }

    public async Task DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();

        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
