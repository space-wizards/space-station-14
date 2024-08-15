using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Xenobiology.UI;

[UsedImplicitly]
public sealed class CellSequencerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CellSequencerWindow? _window;

    public CellSequencerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CellSequencerWindow>();
    }
}
