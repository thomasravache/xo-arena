using Xunit;

namespace TicTacToe.E2E.Journeys.Helpers;

public sealed class NicknameGeneratorTests
{
    [Fact(DisplayName = "SPEC-0065:UT-01 — Dado gerador de apelidos chamado 200 vezes então devolve apelidos únicos de até 20 caracteres")]
    public void Ut01_GeraApelidosUnicosEMenoresQueLimite()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < 200; i++)
        {
            var nick = NicknameGenerator.Generate();
            Assert.False(string.IsNullOrWhiteSpace(nick));
            Assert.True(nick.Length <= 20, $"Apelido '{nick}' excede 20 caracteres (len={nick.Length})");
            Assert.DoesNotContain(nick, set);
            set.Add(nick);
        }

        Assert.Equal(200, set.Count);
    }
}
