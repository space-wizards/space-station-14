using Content.Shared.Humanoid;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.Humanoid;

// Marking BUI.
// Do not use this in any non-privileged instance. This just replaces an entire marking set
// with the set sent over.

[UsedImplicitly]
public sealed class HumanoidMarkingModifierBoundUserInterface(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private HumanoidMarkingModifierWindow? _window;

    private readonly IGameTiming _timing = IoCManager.Resolve<IGameTiming>();

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindowCenteredLeft<HumanoidMarkingModifierWindow>();
        _window.OnMarkingAdded += set => SendPredictedMessage(new HumanoidMarkingModifierMarkingSetMessage(set));
        _window.OnMarkingRemoved += set => SendPredictedMessage(new HumanoidMarkingModifierMarkingSetMessage(set));
        _window.OnMarkingColorChange += set => SendPredictedMessage(new HumanoidMarkingModifierMarkingSetMessage(set));
        _window.OnMarkingRankChange += set => SendPredictedMessage(new HumanoidMarkingModifierMarkingSetMessage(set));
        _window.OnLayerInfoModified += (layer, info) =>
            SendPredictedMessage(new HumanoidMarkingModifierBaseLayersSetMessage(layer, info));

        Update();
    }

    public override void Update()
    {
        if (_window == null || !EntMan.TryGetComponent(Owner, out HumanoidAppearanceComponent? comp))
            return;

        // TODO MarkingPicker needs to be rewritten to use something like a
        // ListContainer instead of an ItemList so it doesn't rebuild itself on
        // every update. For now I'll just make this incompatible with two
        // clients modifying a character's markings at the same time since that
        // way the UI at least feels predicted. (This might not be the best
        // interim solution tbh.)
        if (_timing.IsFirstTimePredicted)
            _window.SetState(comp.MarkingSet, comp.Species, comp.Sex, comp.SkinColor, comp.CustomBaseLayers);
    }
}


