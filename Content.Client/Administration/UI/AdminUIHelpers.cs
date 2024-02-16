using System.Threading;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface.Controls;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.Administration.UI;

public static class AdminUIHelpers
{
    private static void ResetButton(Button button, ConfirmationData data)
    {
        data.Cancellation.Cancel();
        button.ModulateSelfOverride = null;
        button.Text = data.OriginalText;
    }

    public static bool RemoveConfirm(Button button, Dictionary<Button, ConfirmationData> confirmations)
    {
        if (confirmations.Remove(button, out var data))
        {
            ResetButton(button, data);
            return true;
        }

        return false;
    }

    public static void RemoveAllConfirms(Dictionary<Button, ConfirmationData> confirmations)
    {
        foreach (var (button, confirmation) in confirmations)
        {
            ResetButton(button, confirmation);
        }

        confirmations.Clear();
    }

    public static bool TryConfirm(Button button, Dictionary<Button, ConfirmationData> confirmations)
    {
        if (RemoveConfirm(button, confirmations))
            return true;

        var data = new ConfirmationData(new CancellationTokenSource(), button.Text);
        confirmations[button] = data;

        Timer.Spawn(TimeSpan.FromSeconds(5), () =>
        {
            confirmations.Remove(button);
            button.ModulateSelfOverride = null;
            button.Text = data.OriginalText;
        }, data.Cancellation.Token);

        button.ModulateSelfOverride = StyleNano.ButtonColorCautionDefault;
        button.Text = Loc.GetString("admin-player-actions-confirm");
        return false;
    }
}

public readonly record struct ConfirmationData(CancellationTokenSource Cancellation, string? OriginalText);
