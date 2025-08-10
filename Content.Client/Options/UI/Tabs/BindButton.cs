using Robust.Client.Input;
using Robust.Client.UserInterface;
using System.Numerics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.Options.UI.Tabs;

public sealed class BindButton : Button
{
    private IKeyBinding? _binding;

    public Action<GUIBoundKeyEventArgs, IKeyBinding>? ButtonOnKeyBindingDown;

    public BindButton()
    {
        UpdateText();

        MinSize = new Vector2(170, 0);
        OnKeyBindDown += InvokeKeyBindDown;
    }

    public IKeyBinding? Binding
    {
        private get => _binding;
        set
        {
            _binding = value;
            UpdateText();
            if (value != null)
                Disabled = false;
        }
    }

    public void UpdateText()
    {
        Text = Binding?.GetKeyString()
               ?? Loc.GetString("ui-options-unbound");
    }

    private void InvokeKeyBindDown(GUIBoundKeyEventArgs arg)
    {
        if (arg.Function == EngineKeyFunctions.UIRightClick
            && _binding != null)
            Disabled = true;

        if (_binding is not null && ButtonOnKeyBindingDown is not null)
            ButtonOnKeyBindingDown.Invoke(arg, _binding);
    }
}
