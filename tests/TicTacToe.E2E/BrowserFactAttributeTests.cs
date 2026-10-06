using Xunit;

namespace TicTacToe.E2E;

public sealed class BrowserFactAttributeTests
{
    [Fact(DisplayName = "SPEC-0064:UT-01 — Dado BrowserFactAttribute sem E2E_BROWSER=1, então Skip contém instrução")]
    public void Ut01_QuandoVariavelNaoForUm_SkipContemInstrucao()
    {
        var attrNull = new BrowserFactAttribute((string?)null);
        var attrZero = new BrowserFactAttribute("0");
        var attrEmpty = new BrowserFactAttribute(string.Empty);

        Assert.Equal(BrowserFactAttribute.SkipReason, attrNull.Skip);
        Assert.Equal(BrowserFactAttribute.SkipReason, attrZero.Skip);
        Assert.Equal(BrowserFactAttribute.SkipReason, attrEmpty.Skip);
    }

    [Fact(DisplayName = "SPEC-0064:UT-01 — Dado BrowserFactAttribute com E2E_BROWSER=1, então Skip é nulo")]
    public void Ut01_QuandoVariavelForUm_SkipENulo()
    {
        var attr = new BrowserFactAttribute("1");

        Assert.Null(attr.Skip);
    }
}
