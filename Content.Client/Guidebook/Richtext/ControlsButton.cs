using System.Diagnostics.CodeAnalysis;
using Content.Client.Options.UI;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Guidebook.Richtext;

public sealed class ControlsButton : Button, ITag
{
    public bool TryParseTag(List<string> args, Dictionary<string, string> param, [NotNullWhen(true)] out Control? control, out bool instant)
    {
        instant = true;
        control = this;
        Text = Loc.GetString("ui-info-button-controls");
        OnPressed += _ => new OptionsMenu().Open();

        return true;
    }
}
