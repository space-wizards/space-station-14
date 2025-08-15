using System.Numerics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Options.UI.Tabs;

/// <summary>
/// Button for assigning keybinding.
/// </summary>
public sealed class BindButton : Button
{
    private IKeyBinding? _binding;

    public Action<GUIBoundKeyEventArgs, IKeyBinding?>? ButtonOnKeyBindingDown;

    public BindButton()
    {
        UpdateText();

        MinSize = new Vector2(170, 0);
        OnKeyBindDown += InvokeKeyBindDown;
    }

    /// <summary>
    /// Keybinding set to this button.
    /// </summary>
    public IKeyBinding? Binding
    {
        get => _binding;
        set
        {
            _binding = value;
            UpdateText();
            if (value != null)
                Disabled = false;
        }
    }

    /// <summary>
    /// Update button text. Defaults to 'Unbound' text if no binding is present.
    /// </summary>
    public void UpdateText()
    {
        Text = Binding?.GetKeyString()
               ?? Loc.GetString("ui-options-unbound");
    }

    private void InvokeKeyBindDown(GUIBoundKeyEventArgs arg)
    {
        if (_binding is not null && ButtonOnKeyBindingDown is not null)
            ButtonOnKeyBindingDown.Invoke(arg, _binding);
    }
}
