using JetBrains.Annotations;
using Content.Shared.Stacks;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Stack;

[UsedImplicitly]
public sealed class StackCustomSplitBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private StackCustomSplitWindow? _window;

    public StackCustomSplitBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<StackCustomSplitWindow>();

        if (EntMan.TryGetComponent<StackComponent>(Owner, out var comp))
            _window.SetMax(comp.Count);

        _window.ApplyButton.OnPressed += _ =>
        {
            if (int.TryParse(_window.AmountLineEdit.Text, out var i))
            {
                SendMessage(new StackCustomSplitAmountMessage(i));
                _window.Close();
            }
        };
    }
}
