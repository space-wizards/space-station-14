using Content.Server.Actions;
using Content.Server.Humanoid;
using Content.Shared.Corvax.Wagging;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Toggleable;
using Robust.Shared.Prototypes;

namespace Content.Server.Corvax.Wagging;

/// <summary>
/// Adds action to toggle wagging animation for species that support that
/// </summary>
public sealed class WaggingSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("wag");

        SubscribeLocalEvent<WaggingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<WaggingComponent, ToggleActionEvent>(OnToggleAction);
    }

    private void OnStartup(EntityUid uid, WaggingComponent component, ComponentStartup args)
    {
        _action.AddAction(uid, component.ToggleAction, null);
    }

    private void OnToggleAction(EntityUid uid, WaggingComponent component, ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoid) &&
            humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings))
        {
            component.Wagging = !component.Wagging;
            for (var idx = 0; idx < markings.Count; idx++) // Animate all possible tails
            {
                var currentMarkingId = markings[idx].MarkingId;
                var newMarkingId = component.Wagging ? $"{currentMarkingId}Animated" : currentMarkingId.Replace("Animated", "");
                if (!_prototype.HasIndex<MarkingPrototype>(newMarkingId))
                {
                    _sawmill.Warning($"{ToPrettyString(uid)} tried toggle wagging but {newMarkingId} marking not exist");
                    continue;
                }

                _humanoidAppearance.SetMarkingId(uid, MarkingCategories.Tail, idx, newMarkingId,
                    humanoid: humanoid);
            }
        }

        args.Handled = true;
    }
}

