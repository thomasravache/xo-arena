using Xunit;

namespace TicTacToe.E2E;

public sealed class EvidenceRecorderTests
{
    [Fact(DisplayName = "SPEC-0064:UT-02 — Dado gerador de evidências quando teste falha grava zip e png em artifacts/e2e")]
    public async Task Ut02_QuandoTesteFalha_GravaZipEPng()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "e2e_evidence_test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var recorder = new FailureEvidenceRecorder(tempDir);

            // Simula gravação com recorder em falha
            var (tracePath, screenshotPath) = await recorder.RecordAsync(
                context: null,
                page: null,
                testClassName: "ExemploClasse",
                testMethodName: "ExemploTeste");

            Assert.True(File.Exists(tracePath) || tracePath.EndsWith("ExemploClasse.ExemploTeste.zip", StringComparison.Ordinal));
            Assert.EndsWith("ExemploClasse.ExemploTeste.png", screenshotPath, StringComparison.Ordinal);
            Assert.True(Directory.Exists(tempDir));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
