using Xunit;

namespace TicTacToe.E2E;

[Collection("browser")]
public sealed class PlayerIsolationTests
{
    private readonly BrowserFixture _fixture;

    public PlayerIsolationTests(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [BrowserFact(DisplayName = "SPEC-0064:IT-02 — Dados dois PlayerSession do mesmo fixture, quando um grava apelido no armazenamento local, o outro não o enxerga")]
    public async Task It02_JogadoresIsoladosNaoCompartilhamLocalStorage()
    {
        await using var player1 = await _fixture.NewPlayerAsync("Jogador1");
        await using var player2 = await _fixture.NewPlayerAsync("Jogador2");

        await player1.GotoAsync("/");
        await player2.GotoAsync("/");

        // Grava apelido no localStorage do jogador 1
        await player1.Page.EvaluateAsync("() => localStorage.setItem('player_nickname', 'Alice')");

        var val1 = await player1.Page.EvaluateAsync<string>("() => localStorage.getItem('player_nickname')");
        Assert.Equal("Alice", val1);

        // Jogador 2 em contexto isolado não deve enxergar o valor
        var val2 = await player2.Page.EvaluateAsync<string?>("() => localStorage.getItem('player_nickname')");
        Assert.Null(val2);
    }
}
