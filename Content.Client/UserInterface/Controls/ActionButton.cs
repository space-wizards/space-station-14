using Content.Client.HUD;
using Content.Client.Stylesheets;
using Content.Shared.Actions.ActionTypes;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.Controls;

public sealed class ActionButton : SlotControl
{
    private readonly Label _label;

    public BoundKeyFunction? KeyBind
    {
        set
        {
            _keybind = value;
            if (_keybind != null)
            {
                _label.Text = BoundKeyHelper.ShortKeyName(_keybind.Value);
            }
        }
    }

    private BoundKeyFunction? _keybind;

    public ActionButton()
    {
        ButtonRect.Modulate = new(255, 255, 255, 150);;
        ButtonTexturePath = "SlotBackground";
        _label = new Label
        {
            HorizontalAlignment = HAlignment.Left,
            VerticalAlignment = VAlignment.Top
        };
        _label.FontColorOverride = StyleNano.NanoGold;
        AddChild(_label);
    }
}
