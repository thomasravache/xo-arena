using Microsoft.Data.SqlClient;
using Xunit;

namespace TicTacToe.E2E;

[Collection("browser")]
public sealed class DatabaseMigrationTests
{
    private readonly BrowserFixture _fixture;

    public DatabaseMigrationTests(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [BrowserFact(DisplayName = "SPEC-0064:IT-01 — Dado o ambiente recém-subido pelo BrowserFixture, quando consulta __EFMigrationsHistory no SQL Server real, todas as migrations estão aplicadas")]
    public async Task It01_TodasMigrationsAplicadasNoSqlServerReal()
    {
        var connString = await _fixture.GetSqlConnectionStringAsync();
        Assert.NotNull(connString);

        await using var connection = new SqlConnection(connString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory";

        var appliedMigrations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            appliedMigrations.Add(reader.GetString(0));
        }

        var expectedMigrations = new[]
        {
            "20260929130250_InitialCreate",
            "20260929171458_AddMatchDetails",
            "20260929172457_AddPlayerIdentity",
            "20260929190000_AddSeriesInfo",
            "20260929200000_AddGameType",
            "20260929210000_AddChessInfo"
        };

        foreach (var expected in expectedMigrations)
        {
            Assert.Contains(expected, appliedMigrations);
        }
    }
}
