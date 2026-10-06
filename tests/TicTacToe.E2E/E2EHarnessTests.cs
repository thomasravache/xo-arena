using System.Xml.Linq;
using Xunit;

namespace TicTacToe.E2E;

public sealed class E2EHarnessTests
{
    [Fact(DisplayName = "ADR-0013 / SPEC-0064: Nenhum projeto de produção referencia o projeto TicTacToe.E2E")]
    public void RegraArquitetura_NenhumProjetoDeProducaoReferenciaE2E()
    {
        var repoRoot = FindRepoRoot();
        var srcDir = Path.Combine(repoRoot, "src");

        var csprojFiles = Directory.GetFiles(srcDir, "*.csproj", SearchOption.AllDirectories);
        foreach (var file in csprojFiles)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("TicTacToe.E2E", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact(DisplayName = "ADR-0013 / SPEC-0064: TicTacToe.E2E referencia apenas TicTacToe.AppHost dentre os projetos de src")]
    public void RegraArquitetura_E2EReferenciaApenasAppHost()
    {
        var repoRoot = FindRepoRoot();
        var e2eCsproj = Path.Combine(repoRoot, "tests", "TicTacToe.E2E", "TicTacToe.E2E.csproj");
        Assert.True(File.Exists(e2eCsproj), "TicTacToe.E2E.csproj deve existir");

        var doc = XDocument.Load(e2eCsproj);
        var projectReferences = doc.Descendants("ProjectReference")
            .Select(x => x.Attribute("Include")?.Value ?? string.Empty)
            .ToList();

        Assert.Single(projectReferences);
        Assert.Contains("TicTacToe.AppHost.csproj", projectReferences[0], StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null && !File.Exists(Path.Combine(current.FullName, "TicTacToe.sln")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new InvalidOperationException("Raiz do repositório não encontrada.");
    }
}
