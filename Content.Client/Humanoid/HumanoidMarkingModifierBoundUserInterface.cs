using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;

namespace Content.Client.Humanoid;

// Marking BUI.
// Do not use this in any non-privileged instance. This just replaces an entire marking set
// with the set sent over.

public sealed class HumanoidMarkingModifierBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private HumanoidMarkingModifierWindow? _window;

    public HumanoidMarkingModifierBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new();
        _window.OnClose += Close;
        _window.OnMarkingAdded += SendMarkingSet;
        _window.OnMarkingRemoved += SendMarkingSet;
        _window.OnMarkingColorChange += SendMarkingSetNoResend;
        _window.OnMarkingRankChange += SendMarkingSet;
        _window.OnLayerInfoModified += SendBaseLayer;

        _window.OpenCenteredLeft();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not HumanoidMarkingModifierState cast)
        {
            return;
        }

        _window.SetState(cast.MarkingSet, cast.Species, cast.Sex, cast.SkinColor, cast.CustomBaseLayers);
    }

    private void SendMarkingSet(MarkingSet set)
    {
        SendMessage(new HumanoidMarkingModifierMarkingSetMessage(set, true));
    }

    private void SendMarkingSetNoResend(MarkingSet set)
    {
        SendMessage(new HumanoidMarkingModifierMarkingSetMessage(set, false));
    }

    private void SendBaseLayer(HumanoidVisualLayers layer, CustomBaseLayerInfo? info)
    {
        SendMessage(new HumanoidMarkingModifierBaseLayersSetMessage(layer, info, true));
    }
}


