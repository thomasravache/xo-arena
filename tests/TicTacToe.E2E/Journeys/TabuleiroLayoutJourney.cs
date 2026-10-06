using Microsoft.Playwright;
using TicTacToe.E2E.Journeys.Helpers;
using Xunit;

namespace TicTacToe.E2E.Journeys;

[Collection("browser")]
public sealed class TabuleiroLayoutJourney
{
    private readonly BrowserFixture _fixture;

    public TabuleiroLayoutJourney(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [BrowserFact(DisplayName = "SPEC-0065:E2E-04 — Dado o tabuleiro de xadrez na arena, mede entre 600 e 672 px em 1440x900 e não gera rolagem horizontal em 390x844")]
    public async Task E2E04_TabuleiroDimensoesDesktopEMobile()
    {
        // 1. Cenário Desktop: Viewport 1440x900
        await using var desktopPlayer = await _fixture.NewPlayerAsync("DesktopUser", new ViewportSize { Width = 1440, Height = 900 });
        await desktopPlayer.GotoAsync("/xadrez");

        await desktopPlayer.Page.Locator("#playerName").FillAsync(NicknameGenerator.Generate("Dsk"));
        await desktopPlayer.Page.Locator("button:has-text('Iniciar partida solo')").ClickAsync();

        var desktopBoard = desktopPlayer.Page.Locator("[data-board]");
        await Assertions.Expect(desktopBoard).ToBeVisibleAsync();

        var desktopBox = await desktopBoard.BoundingBoxAsync();
        Assert.NotNull(desktopBox);
        Assert.InRange(desktopBox.Width, 600, 672);

        // 2. Cenário Celular: Viewport 390x844
        await using var mobilePlayer = await _fixture.NewPlayerAsync("MobileUser", new ViewportSize { Width = 390, Height = 844 });
        await mobilePlayer.GotoAsync("/xadrez");

        await mobilePlayer.Page.Locator("#playerName").FillAsync(NicknameGenerator.Generate("Mob"));
        await mobilePlayer.Page.Locator("button:has-text('Iniciar partida solo')").ClickAsync();

        var mobileBoard = mobilePlayer.Page.Locator("[data-board]");
        await Assertions.Expect(mobileBoard).ToBeVisibleAsync();

        var mobileBox = await mobileBoard.BoundingBoxAsync();
        Assert.NotNull(mobileBox);
        Assert.True(mobileBox.Width <= 390, $"Largura do tabuleiro ({mobileBox.Width}px) deve caber no viewport de 390px");

        // Verifica ausência de rolagem horizontal na janela do celular
        var hasHorizontalScroll = await mobilePlayer.Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth");
        Assert.False(hasHorizontalScroll, "A página não deve ter rolagem horizontal no celular (390px)");
    }
}
