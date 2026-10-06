using Microsoft.Data.SqlClient;
using Xunit;

namespace TicTacToe.E2E.Journeys;

[Collection("browser")]
public sealed class HistoricoXadrezIntegrationTests
{
    private readonly BrowserFixture _fixture;

    public HistoricoXadrezIntegrationTests(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [BrowserFact(DisplayName = "SPEC-0065:IT-01 — Dado o SQL Server real iniciado pelo BrowserFixture, quando uma partida de xadrez com dados de AddGameType e AddChessInfo é persistida em Gameplay.MatchResults, então a consulta retorna os campos e enums com integridade")]
    public async Task It01_PersistenciaEIntegridadeDeXadrezNoSqlServerReal()
    {
        var connString = await _fixture.GetSqlConnectionStringAsync();
        Assert.NotNull(connString);

        await using var connection = new SqlConnection(connString);
        await connection.OpenAsync();

        var matchId = Guid.NewGuid();
        var whitePlayer = "White_" + Guid.NewGuid().ToString("N")[..8];
        var blackPlayer = "Black_" + Guid.NewGuid().ToString("N")[..8];
        const string movesSan = "f3 e5 g4 Qh4#";
        const string finalFen = "rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 1 3";
        const string timeControl = "blitz5+0";
        const int gameTypeChess = 1; // GameType.Chess
        const string reasonCheckmate = "Checkmate"; // EndReason.Checkmate é gravado como string no EF Core
        const int gameModeOnline = 0; // GameMode.Online

        await using (var insertCmd = connection.CreateCommand())
        {
            insertCmd.CommandText = @"
                INSERT INTO Gameplay.MatchResults
                (Id, PlayerXName, PlayerOName, WinnerName, PlayedAt, DurationSeconds, MoveCount, EndReason, WinnerSide, Mode, GameType, TimeControl, MovesSan, FinalFen)
                VALUES
                (@Id, @White, @Black, @Winner, @PlayedAt, @Duration, @MoveCount, @Reason, @Side, @Mode, @GameType, @TimeControl, @MovesSan, @FinalFen);";

            insertCmd.Parameters.AddWithValue("@Id", matchId);
            insertCmd.Parameters.AddWithValue("@White", whitePlayer);
            insertCmd.Parameters.AddWithValue("@Black", blackPlayer);
            insertCmd.Parameters.AddWithValue("@Winner", blackPlayer);
            insertCmd.Parameters.AddWithValue("@PlayedAt", DateTime.UtcNow);
            insertCmd.Parameters.AddWithValue("@Duration", 42);
            insertCmd.Parameters.AddWithValue("@MoveCount", 4);
            insertCmd.Parameters.AddWithValue("@Reason", reasonCheckmate);
            insertCmd.Parameters.AddWithValue("@Side", "O");
            insertCmd.Parameters.AddWithValue("@Mode", gameModeOnline);
            insertCmd.Parameters.AddWithValue("@GameType", gameTypeChess);
            insertCmd.Parameters.AddWithValue("@TimeControl", timeControl);
            insertCmd.Parameters.AddWithValue("@MovesSan", movesSan);
            insertCmd.Parameters.AddWithValue("@FinalFen", finalFen);

            var rows = await insertCmd.ExecuteNonQueryAsync();
            Assert.Equal(1, rows);
        }

        await using (var selectCmd = connection.CreateCommand())
        {
            selectCmd.CommandText = @"
                SELECT GameType, FinalFen, MovesSan, TimeControl, EndReason, WinnerName
                FROM Gameplay.MatchResults
                WHERE Id = @Id;";
            selectCmd.Parameters.AddWithValue("@Id", matchId);

            await using var reader = await selectCmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync(), "Registro de partida de xadrez deve existir");

            Assert.Equal(gameTypeChess, reader.GetInt32(0));
            Assert.Equal(finalFen, reader.GetString(1));
            Assert.Equal(movesSan, reader.GetString(2));
            Assert.Equal(timeControl, reader.GetString(3));
            Assert.Equal(reasonCheckmate, reader.GetString(4));
            Assert.Equal(blackPlayer, reader.GetString(5));
        }
    }
}
