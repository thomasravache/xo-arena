using Microsoft.Playwright;
using TicTacToe.E2E.Journeys.Helpers;
using Xunit;

namespace TicTacToe.E2E.Journeys;

[Collection("browser")]
public sealed class XadrezDueloJourney
{
    private readonly BrowserFixture _fixture;

    public XadrezDueloJourney(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [BrowserFact(DisplayName = "SPEC-0065:E2E-03 — Dados dois jogadores na fila Blitz, jogam mate do tolo até o fim e partida aparece no histórico do banco real")]
    public async Task E2E03_DueloDeXadrezAteXequeMateEHistoricoGravadoNoBancoReal()
    {
        var whiteNick = NicknameGenerator.Generate("Brc");
        var blackNick = NicknameGenerator.Generate("Prt");

        await using var p1 = await _fixture.NewPlayerAsync(whiteNick);
        await using var p2 = await _fixture.NewPlayerAsync(blackNick);

        // 1. Jogador 1 entra na fila preferindo Brancas
        await p1.GotoAsync("/xadrez");
        await LobbyHelper.SetPlayerNameAsync(p1.Page, whiteNick);
        await p1.Page.Locator("button:has-text('Brancas')").ClickAsync();
        await p1.Page.Locator("button:has-text('Procurar oponente')").ClickAsync();

        // 2. Jogador 2 entra na fila preferindo Pretas
        await p2.GotoAsync("/xadrez");
        await LobbyHelper.SetPlayerNameAsync(p2.Page, blackNick);
        await p2.Page.Locator("button:has-text('Pretas')").ClickAsync();
        await p2.Page.Locator("button:has-text('Procurar oponente')").ClickAsync();

        // 3. Aguarda o pareamento e o carregamento do tabuleiro para ambos os jogadores
        await Assertions.Expect(p1.Page.Locator("[data-board]")).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Assertions.Expect(p2.Page.Locator("[data-board]")).ToBeVisibleAsync(new() { Timeout = 15000 });

        // Identifica com segurança quem comanda as Brancas e as Pretas
        var p1TurnText = await p1.Page.Locator("[data-notice='turn']").InnerTextAsync();
        PlayerSession whitePlayer;
        PlayerSession blackPlayer;
        string winnerName;

        if (p1TurnText.Contains(whiteNick, StringComparison.OrdinalIgnoreCase))
        {
            whitePlayer = p1;
            blackPlayer = p2;
            winnerName = blackNick;
        }
        else
        {
            whitePlayer = p2;
            blackPlayer = p1;
            winnerName = whiteNick;
        }

        // 4. Executa os 4 lances do Mate do Tolo (1.f3 e5 2.g4 Dh4#)
        // Lance 1: Brancas jogam f2 -> f3
        await whitePlayer.Page.Locator("button[data-square='f2']").ClickAsync();
        await whitePlayer.Page.Locator("button[data-square='f3']").ClickAsync();
        await Assertions.Expect(blackPlayer.Page.Locator("[data-move-list]")).ToContainTextAsync("f3", new() { Timeout = 10000 });

        // Lance 2: Pretas jogam e7 -> e5
        await blackPlayer.Page.Locator("button[data-square='e7']").ClickAsync();
        await blackPlayer.Page.Locator("button[data-square='e5']").ClickAsync();
        await Assertions.Expect(whitePlayer.Page.Locator("[data-move-list]")).ToContainTextAsync("e5", new() { Timeout = 10000 });

        // Lance 3: Brancas jogam g2 -> g4
        await whitePlayer.Page.Locator("button[data-square='g2']").ClickAsync();
        await whitePlayer.Page.Locator("button[data-square='g4']").ClickAsync();
        await Assertions.Expect(blackPlayer.Page.Locator("[data-move-list]")).ToContainTextAsync("g4", new() { Timeout = 10000 });

        // Lance 4: Pretas jogam d8 -> h4 (Dh4#)
        await blackPlayer.Page.Locator("button[data-square='d8']").ClickAsync();
        await blackPlayer.Page.Locator("button[data-square='h4']").ClickAsync();

        // 5. Verifica que ambos os jogadores visualizam o card de fim de jogo com xeque-mate
        var p1EndTitle = p1.Page.Locator("[data-end-title]");
        var p2EndTitle = p2.Page.Locator("[data-end-title]");

        await Assertions.Expect(p1EndTitle).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Assertions.Expect(p2EndTitle).ToBeVisibleAsync(new() { Timeout = 10000 });

        await Assertions.Expect(p1EndTitle).ToContainTextAsync("Xeque-mate");
        await Assertions.Expect(p1EndTitle).ToContainTextAsync(winnerName);

        await Assertions.Expect(p2EndTitle).ToContainTextAsync("Xeque-mate");
        await Assertions.Expect(p2EndTitle).ToContainTextAsync(winnerName);

        // 6. Confere que a partida finalizada está gravada no SQL Server real acessando o histórico
        await p1.GotoAsync("/history?jogo=xadrez");
        var scopeAll = p1.Page.Locator("button[role='radio']:has-text('Todos')");
        if (await scopeAll.IsVisibleAsync())
        {
            await scopeAll.ClickAsync();
        }
        var historyCell = p1.Page.Locator("td[data-cell='duelo']").First;

        await Assertions.Expect(historyCell).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Assertions.Expect(historyCell).ToContainTextAsync(whiteNick);
        await Assertions.Expect(historyCell).ToContainTextAsync(blackNick);
    }
}
