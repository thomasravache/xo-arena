using Microsoft.Playwright;
using Xunit;

namespace TicTacToe.E2E.Journeys;

[Collection("browser")]
public sealed class SelecaoDeJogosJourney
{
    private readonly BrowserFixture _fixture;

    public SelecaoDeJogosJourney(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [BrowserFact(DisplayName = "SPEC-0065:E2E-01 — Dado o app no navegador, quando abre / e navega para /xadrez e /velha, então o item Jogar fica ativo em todas")]
    public async Task E2E01_SelecaoDeJogosNavegaParaLobbiesEItemJogarFicaAtivo()
    {
        await using var player = await _fixture.NewPlayerAsync("Visitante");

        // 1. Abre a raiz /
        await player.GotoAsync("/");
        var page = player.Page;

        // Verifica que o item 'Jogar' está ativo na barra de navegação
        var activePlayNavItem = page.Locator("nav[aria-label='Principal'] a[href='/']");
        await Assertions.Expect(activePlayNavItem).ToHaveAttributeAsync("aria-current", "page");

        // 2. Clica no card de Xadrez e navega para /xadrez
        var chessCard = page.Locator("a[aria-label='Jogar Xadrez']");
        await Assertions.Expect(chessCard).ToBeVisibleAsync();
        await chessCard.ClickAsync();

        await page.WaitForURLAsync("**/xadrez");
        Assert.Contains("/xadrez", page.Url);
        await Assertions.Expect(activePlayNavItem).ToHaveAttributeAsync("aria-current", "page");

        // 3. Volta para / e clica no card de Jogo da Velha
        await player.GotoAsync("/");
        var velhaCard = page.Locator("a[aria-label='Jogar Jogo da Velha']");
        await Assertions.Expect(velhaCard).ToBeVisibleAsync();
        await velhaCard.ClickAsync();

        await page.WaitForURLAsync("**/velha");
        Assert.Contains("/velha", page.Url);
        await Assertions.Expect(activePlayNavItem).ToHaveAttributeAsync("aria-current", "page");
    }
}
