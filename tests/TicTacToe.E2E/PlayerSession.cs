using Microsoft.Playwright;

namespace TicTacToe.E2E;

/// <summary>
/// Representa a sessão de um jogador em um contexto isolado de navegador (cookies, localStorage, cache).
/// </summary>
public sealed class PlayerSession : IAsyncDisposable
{
    private readonly IBrowserContext _context;
    private readonly Uri _baseUri;
    private readonly FailureEvidenceRecorder? _evidenceRecorder;
    private bool _disposed;

    public IPage Page { get; }
    public string Name { get; }

    public PlayerSession(
        IBrowserContext context,
        IPage page,
        Uri baseUri,
        string name,
        FailureEvidenceRecorder? evidenceRecorder = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Page = page ?? throw new ArgumentNullException(nameof(page));
        _baseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _evidenceRecorder = evidenceRecorder;
    }

    /// <summary>
    /// Navega relativo à BaseUri e aguarda o carregamento do DOM.
    /// </summary>
    public async Task GotoAsync(string path)
    {
        var targetUri = new Uri(_baseUri, path);
        await Page.GotoAsync(targetUri.ToString());
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    public async Task CaptureEvidenceOnFailureAsync(string testClassName, string testMethodName)
    {
        if (_evidenceRecorder != null)
        {
            await _evidenceRecorder.RecordAsync(_context, Page, testClassName, testMethodName);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _context.CloseAsync();
        await _context.DisposeAsync();
    }
}
