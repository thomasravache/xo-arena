using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace TicTacToe.E2E;

[CollectionDefinition("browser")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Convenção xUnit para fixture de coleção")]
public sealed class BrowserCollection : ICollectionFixture<BrowserFixture>
{
}
