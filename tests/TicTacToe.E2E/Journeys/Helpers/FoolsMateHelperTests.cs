using Xunit;

namespace TicTacToe.E2E.Journeys.Helpers;

public sealed class FoolsMateHelperTests
{
    [Fact(DisplayName = "SPEC-0065:UT-02 — Dada notação de lances do mate do tolo quando converte em pares de casas então produz coordenadas corretas")]
    public void Ut02_ConverteLancesDoMateDoToloEmParesDeCasas()
    {
        var input = new[] { "f3", "e5", "g4", "Dh4" };
        var moves = FoolsMateHelper.ParseMoves(input);

        Assert.Equal(4, moves.Count);
        Assert.Equal(("f2", "f3"), moves[0]);
        Assert.Equal(("e7", "e5"), moves[1]);
        Assert.Equal(("g2", "g4"), moves[2]);
        Assert.Equal(("d8", "h4"), moves[3]);
    }

    [Fact(DisplayName = "SPEC-0065:UT-02 — Lista estática de lances do mate do tolo contém a sequência exata de 4 lances")]
    public void Ut02_ListaEstaticaContemSequenciaExata()
    {
        var moves = FoolsMateHelper.Moves;

        Assert.Equal(4, moves.Count);
        Assert.Equal(("f2", "f3"), moves[0]);
        Assert.Equal(("e7", "e5"), moves[1]);
        Assert.Equal(("g2", "g4"), moves[2]);
        Assert.Equal(("d8", "h4"), moves[3]);
    }
}
