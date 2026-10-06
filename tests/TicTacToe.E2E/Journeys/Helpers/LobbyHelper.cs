using Microsoft.Playwright;

namespace TicTacToe.E2E.Journeys.Helpers;

public static class LobbyHelper
{
    public static async Task SetPlayerNameAsync(IPage page, string nickname)
    {
        var nameInput = page.Locator("#playerName");
        await nameInput.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await nameInput.FillAsync(nickname);

        var readyChip = page.Locator("text=Pronto para jogar");
        try
        {
            await Assertions.Expect(readyChip).ToBeVisibleAsync(new() { Timeout = 3000 });
        }
        catch
        {
            // Caso o ciclo inicial do Blazor tenha re-renderizado o componente, re-preenche
            await nameInput.ClickAsync();
            await nameInput.FillAsync(nickname);
            await Assertions.Expect(readyChip).ToBeVisibleAsync(new() { Timeout = 12000 });
        }
    }
}
