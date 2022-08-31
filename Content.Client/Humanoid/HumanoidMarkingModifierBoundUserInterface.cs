using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;

namespace Content.Client.Humanoid;

// Marking BUI.
// Do not use this in any non-privileged instance. This just replaces an entire marking set
// with the set sent over.

public sealed class HumanoidMarkingModifierBoundUserInterface : BoundUserInterface
{
    public HumanoidMarkingModifierBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    private HumanoidMarkingModifierWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = new();
        _window.OnClose += Close;
        _window.OnMarkingAdded += SendMarkingSet;
        _window.OnMarkingRemoved += SendMarkingSet;
        _window.OnMarkingColorChange += SendMarkingSetNoResend;
        _window.OnMarkingRankChange += SendMarkingSet;

        _window.OpenCenteredLeft();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not HumanoidMarkingModifierState cast)
        {
            return;
        }

        _window.SetMarkings(cast.MarkingSet, cast.Species, cast.SkinColor);
    }

    private void SendMarkingSet(MarkingSet set)
    {
        SendMessage(new HumanoidMarkingModifierMarkingSetMessage(set, true));
    }

    private void SendMarkingSetNoResend(MarkingSet set)
    {
        SendMessage(new HumanoidMarkingModifierMarkingSetMessage(set, false));
    }
}


