using Xunit;

namespace TicTacToe.E2E;

public sealed class DockerValidatorTests
{
    [Fact(DisplayName = "SPEC-0064:UT-03 — Dado verificador de Docker quando daemon não responde então lança erro com mensagem Inicie o Docker")]
    public async Task Ut03_QuandoDaemonNaoResponde_LancaErroComMensagem()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DockerValidator.EnsureDockerAvailableAsync(_ => Task.FromResult(false), TimeSpan.FromMilliseconds(500)));

        Assert.Contains(DockerValidator.DockerNotAvailableMessage, ex.Message);
    }

    [Fact(DisplayName = "SPEC-0064:UT-03 — Dado verificador de Docker quando ocorre timeout então lança erro rápido com instrução")]
    public async Task Ut03_QuandoOcorreTimeout_LancaErroEmAteTimeout()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DockerValidator.EnsureDockerAvailableAsync(async ct =>
            {
                await Task.Delay(2000, ct);
                return true;
            }, TimeSpan.FromMilliseconds(100)));

        Assert.Contains(DockerValidator.DockerNotAvailableMessage, ex.Message);
    }

    [Fact(DisplayName = "SPEC-0064:UT-03 — Dado verificador de Docker quando daemon responde então conclui com sucesso")]
    public async Task Ut03_QuandoDaemonResponde_ConcluiComSucesso()
    {
        await DockerValidator.EnsureDockerAvailableAsync(_ => Task.FromResult(true), TimeSpan.FromSeconds(1));
    }
}
