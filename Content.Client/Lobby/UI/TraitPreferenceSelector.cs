using Content.Shared.Traits;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

internal sealed class TraitPreferenceSelector : Control
{
    public TraitPrototype Trait { get; }
    private readonly CheckBox _checkBox;

    public bool Preference
    {
        get => _checkBox.Pressed;
        set => _checkBox.Pressed = value;
    }

    public event Action<bool>? PreferenceChanged;

    public TraitPreferenceSelector(TraitPrototype trait)
    {
        Trait = trait;

        _checkBox = new CheckBox {Text = Loc.GetString(trait.Name)};
        _checkBox.OnToggled += OnCheckBoxToggled;

        if (trait.Description is { } desc)
        {
            _checkBox.ToolTip = Loc.GetString(desc);
        }

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Children = { _checkBox },
        });
    }

    private void OnCheckBoxToggled(BaseButton.ButtonToggledEventArgs args)
    {
        PreferenceChanged?.Invoke(Preference);
    }
}