using System.Linq; //Starlight
using Content.Server.Actions; //Starlight
using Content.Server.Humanoid;
using Content.Shared.Actions.Components; //Starlight
using Content.Shared.GameTicking; //Starlight
using Content.Shared.Cloning.Events;
using Content.Shared._Starlight.Humanoid.Markings;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs;
using Content.Shared.Roles; //Starlight
using Content.Shared.Toggleable;
using Content.Shared.Wagging;
using Robust.Shared.Prototypes;

namespace Content.Server.Wagging;

/// <summary>
/// Adds an action to toggle wagging animation for tails markings that supporting this
/// </summary>
public sealed class WaggingSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [Dependency] private readonly StarlightMarkingSystem _starlightMarking = default!; //starlight edit

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaggingComponent, ComponentInit>(OnComponentInit); //Starlight moved from MapInit to ComponentInit
        SubscribeLocalEvent<WaggingComponent, MarkingsUpdateEvent>(OnMarkingsUpdate); //Starlight
        SubscribeLocalEvent<WaggingComponent, ComponentShutdown>(OnWaggingShutdown);
        SubscribeLocalEvent<WaggingComponent, ToggleActionEvent>(OnWaggingToggle);
        SubscribeLocalEvent<WaggingComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<WaggingComponent, CloningEvent>(OnCloning);
    }

    private void OnCloning(Entity<WaggingComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        EnsureComp<WaggingComponent>(args.CloneUid);
    }

    private void OnComponentInit(EntityUid uid, WaggingComponent component, ComponentInit args) //Starlight moved from MapInit to Component Init
    {
         _actions.AddAction(uid, ref component.ActionEntity, component.Action, uid);
    }

    #region Starlight
    private void OnMarkingsUpdate(EntityUid uid, WaggingComponent component, MarkingsUpdateEvent args)
    {
        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
        {
            if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings))
            {
                if (!_actions.GetAction(component.ActionEntity).HasValue)
                {
                    _actions.AddAction(uid, ref component.ActionEntity, component.Action, uid);
                }
            }
        }
    }
    #endregion

    private void OnWaggingShutdown(EntityUid uid, WaggingComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEntity);
    }

    private void OnWaggingToggle(EntityUid uid, WaggingComponent component, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        TryToggleWagging(uid, wagging: component);
    }

    private void OnMobStateChanged(EntityUid uid, WaggingComponent component, MobStateChangedEvent args)
    {
        if (component.Wagging)
            TryToggleWagging(uid, wagging: component);
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
            //starlight for loop
            foreach (var possibleSuffix in wagging.Suffixes)
            {
                var currentMarkingId = markings[idx].MarkingId;
                string? newMarkingId;

                if (wagging.Wagging)
                {
                    newMarkingId = $"{currentMarkingId}{possibleSuffix}"; //starlight edit
                }
                else
                {
                    if (currentMarkingId.EndsWith(possibleSuffix)) //starlight edit
                    {
                        newMarkingId = currentMarkingId[..^possibleSuffix.Length]; //starlight edit
                    }
                    else
                    {
                        newMarkingId = currentMarkingId;
                        Log.Warning($"Unable to revert wagging for {currentMarkingId}");
                    }
                }

                if (!_prototype.HasIndex<MarkingPrototype>(newMarkingId) &&
                    !_starlightMarking.TryGetWaggingId(currentMarkingId, out newMarkingId)) //starlight edit
                {
                    Log.Warning($"{ToPrettyString(uid)} tried toggling wagging but {newMarkingId} marking doesn't exist");
                    continue;
                }

                _humanoidAppearance.SetMarkingId(uid, MarkingCategories.Tail, idx, newMarkingId,
                    humanoid: humanoid);
            }
        }

        return true;
    }
}
