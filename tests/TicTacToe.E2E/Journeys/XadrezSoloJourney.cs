using Microsoft.Playwright;
using TicTacToe.E2E.Journeys.Helpers;
using Xunit;

namespace TicTacToe.E2E.Journeys;

[Collection("browser")]
public sealed class XadrezSoloJourney
{
    private readonly BrowserFixture _fixture;

    public XadrezSoloJourney(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [BrowserFact(DisplayName = "SPEC-0065:E2E-02 — Dado o lobby de xadrez, quando joga solo contra o robô Fácil, lance e2-e4 aparece, robô responde e abandonar volta ao lobby")]
    public async Task E2E02_PartidaSoloContraRoboComLanceERespostaEAbandono()
    {
        await using var player = await _fixture.NewPlayerAsync("JogadorSolo");
        var page = player.Page;

        // 1. Navega para /xadrez
        await player.GotoAsync("/xadrez");

        // 2. Preenche apelido
        var playerName = NicknameGenerator.Generate("Solo");
        await LobbyHelper.SetPlayerNameAsync(page, playerName);

        // 3. Garante Brancas selecionadas
        var whiteOption = page.Locator("button:has-text('Brancas')");
        await whiteOption.ClickAsync();

        // 4. Inicia partida solo contra o robô
        var startSoloBtn = page.Locator("button:has-text('Iniciar partida solo')");
        await startSoloBtn.ClickAsync();

        // 5. Aguarda arena e tabuleiro carregarem
        var board = page.Locator("[data-board]");
        await Assertions.Expect(board).ToBeVisibleAsync();

        // 6. Realiza o lance e2 -> e4 por cliques
        var e2 = page.Locator("button[data-square='e2']");
        var e4 = page.Locator("button[data-square='e4']");

        await e2.ClickAsync();
        await Assertions.Expect(e2).ToHaveAttributeAsync("data-selected", "true");

        await e4.ClickAsync();

        // 7. Confirma que o lance 'e4' apareceu na lista de lances
        var moveList = page.Locator("[data-move-list]");
        await Assertions.Expect(moveList).ToBeVisibleAsync();
        await Assertions.Expect(moveList).ToContainTextAsync("e4");

        // 8. Aguarda o robô responder (a lista passa a conter pelo menos 2 lances e a vez volta para o jogador)
        var turnNotice = page.Locator("[data-notice='turn']");
        await Assertions.Expect(turnNotice).ToContainTextAsync($"Sua vez, {playerName}!", new() { Timeout = 10000 });

        // 9. Clica em Abandonar para voltar ao lobby
        var abandonBtn = page.Locator("button:has-text('Abandonar')");
        await Assertions.Expect(abandonBtn).ToBeVisibleAsync();
        await abandonBtn.ClickAsync();

        // 10. Verifica retorno ao lobby de xadrez
        var lobbyTitle = page.Locator("h1:has-text('Lobby de Xadrez')");
        await Assertions.Expect(lobbyTitle).ToBeVisibleAsync();
    }
}
