namespace TicTacToe.E2E.Journeys.Helpers;

public static class FoolsMateHelper
{
    public static readonly IReadOnlyList<(string From, string To)> Moves =
    [
        ("f2", "f3"),
        ("e7", "e5"),
        ("g2", "g4"),
        ("d8", "h4")
    ];

    public static IReadOnlyList<(string From, string To)> ParseMoves(IEnumerable<string> notations)
    {
        return notations.Select(ParseMove).ToList();
    }

    public static (string From, string To) ParseMove(string san)
    {
        var cleaned = san.Trim().Replace(".", "", StringComparison.Ordinal);
        if (cleaned.StartsWith('1') || cleaned.StartsWith('2'))
        {
            cleaned = cleaned[1..].Trim();
        }

        return cleaned switch
        {
            "f3" or "f2-f3" or "f2f3" => ("f2", "f3"),
            "e5" or "e7-e5" or "e7e5" => ("e7", "e5"),
            "g4" or "g2-g4" or "g2g4" => ("g2", "g4"),
            "Dh4" or "Dh4#" or "Qh4" or "Qh4#" or "d8-h4" or "d8h4" => ("d8", "h4"),
            _ => throw new ArgumentException($"Lance de teste desconhecido: '{san}'", nameof(san))
        };
    }
}
