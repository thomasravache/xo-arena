using Microsoft.Playwright;
using Xunit;

namespace TicTacToe.E2E;

[Collection("browser")]
public sealed class HarnessSmokeTests
{
    private readonly BrowserFixture _fixture;

    public HarnessSmokeTests(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [BrowserFact(DisplayName = "SPEC-0064:E2E-01 — Dado o ambiente real e E2E_BROWSER=1, quando abre / no Chromium, a seleção de jogos aparece com o título Escolha seu jogo e as duas cartas")]
    public async Task E2E01_SelecaoDeJogosAbreNoChromiumComCartas()
    {
        await using var player = await _fixture.NewPlayerAsync("JogadorFumaca");

        await player.GotoAsync("/");

        var title = await player.Page.TitleAsync();
        Assert.Contains("Escolha seu jogo", title);

        var velhaCard = player.Page.Locator("a[aria-label='Jogar Jogo da Velha']");
        var xadrezCard = player.Page.Locator("a[aria-label='Jogar Xadrez']");

        await Assertions.Expect(velhaCard).ToBeVisibleAsync();
        await Assertions.Expect(xadrezCard).ToBeVisibleAsync();
    }
}
