using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.Humanoid;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs;
using Content.Shared.Toggleable;
using Robust.Shared.Prototypes;

namespace Content.Server.Wagging;

/// <summary>
/// Adds an action to toggle wagging animation for tails markings that supporting this
/// </summary>
public sealed class WaggingSystem : EntitySystem
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("wag");

        SubscribeLocalEvent<WaggingComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<WaggingComponent, EmoteEvent>(OnEmote);
    }

    private void OnMobStateChanged(EntityUid uid, WaggingComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (component.Wagging)
            TryToggleWagging(uid, wagging: component);
    }

    private void OnEmote(EntityUid uid, WaggingComponent component, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        if (args.Emote.ID == component.EmoteId)
            args.Handled = TryToggleWagging(uid, component);
    }

    public bool TryToggleWagging(EntityUid uid, WaggingComponent? wagging = null, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref wagging, ref humanoid))
            return false;

        if (!humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings))
            return false;

        if (markings.Count == 0)
            return false;

        wagging.Wagging = !wagging.Wagging;
        for (var idx = 0; idx < markings.Count; idx++) // Animate all possible tails
        {
            var currentMarkingId = markings[idx].MarkingId;
            var newMarkingId = wagging.Wagging ? $"{currentMarkingId}Animated" : currentMarkingId.Replace("Animated", ""); // Ok while tails is marking
            if (!_prototype.HasIndex<MarkingPrototype>(newMarkingId))
            {
                _sawmill.Warning($"{ToPrettyString(uid)} tried toggle wagging but {newMarkingId} marking not exist");
                continue;
            }

            _humanoidAppearance.SetMarkingId(uid, MarkingCategories.Tail, idx, newMarkingId,
                humanoid: humanoid);
        }

        return true;
    }
}
