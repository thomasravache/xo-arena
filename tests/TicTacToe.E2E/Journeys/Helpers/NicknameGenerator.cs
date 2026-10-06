namespace TicTacToe.E2E.Journeys.Helpers;

public static class NicknameGenerator
{
    private static int _counter;

    public static string Generate(string prefix = "Jog")
    {
        var count = Interlocked.Increment(ref _counter);
        var tick = (DateTime.UtcNow.Ticks % 10000).ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
        var name = $"{prefix}{count}_{tick}";
        if (name.Length > 20)
        {
            name = name[..20];
        }

        return name;
    }
}
