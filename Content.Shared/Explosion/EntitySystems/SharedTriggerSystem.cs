using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Explosion.Components;
using Content.Shared.Payload.Components;
using Content.Shared.Trigger;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Shared.Explosion.EntitySystems;

public abstract class SharedTriggerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;

    public bool Trigger(EntityUid trigger, EntityUid? user = null)
    {
        var triggerEvent = new TriggerEvent(trigger, user);
        RaiseLocalEvent(trigger, ref triggerEvent, true);
        return triggerEvent.Handled;
    }

    public void HandleTimerTrigger(EntityUid uid, EntityUid? user, float delay, float beepInterval, float? initialBeepDelay, SoundSpecifier? beepSound)
    {
        if (delay <= 0)
        {
            RemComp<ActiveTimerTriggerComponent>(uid);
            Trigger(uid, user);
            return;
        }

        if (HasComp<ActiveTimerTriggerComponent>(uid))
            return;

        if (user != null)
        {
            // Check if entity is bomb/mod. grenade/etc
            if (_container.TryGetContainer(uid, "payload", out var container) &&
                container.ContainedEntities.Count > 0 &&
                TryComp(container.ContainedEntities[0], out ChemicalPayloadComponent? chemicalPayloadComponent))
            {
                // If a beaker is missing, the entity won't explode, so no reason to log it
                if (chemicalPayloadComponent.BeakerSlotA.Item is not { } beakerA ||
                    chemicalPayloadComponent.BeakerSlotB.Item is not { } beakerB ||
                    !TryComp(beakerA, out SolutionContainerManagerComponent? containerA) ||
                    !TryComp(beakerB, out SolutionContainerManagerComponent? containerB) ||
                    !TryComp(beakerA, out FitsInDispenserComponent? fitsA) ||
                    !TryComp(beakerB, out FitsInDispenserComponent? fitsB) ||
                    !_solutions.TryGetSolution((beakerA, containerA), fitsA.Solution, out _, out var solutionA) ||
                    !_solutions.TryGetSolution((beakerB, containerB), fitsB.Solution, out _, out var solutionB))
                    return;

                _adminLogger.Add(LogType.Trigger,
                    $"{ToPrettyString(user.Value):user} started a {delay} second timer trigger on entity {ToPrettyString(uid):timer}, which contains {SharedSolutionContainerSystem.ToPrettyString(solutionA)} in one beaker and {SharedSolutionContainerSystem.ToPrettyString(solutionB)} in the other.");
            }
            else
            {
                _adminLogger.Add(LogType.Trigger,
                    $"{ToPrettyString(user.Value):user} started a {delay} second timer trigger on entity {ToPrettyString(uid):timer}");
            }

        }
        else
        {
            _adminLogger.Add(LogType.Trigger,
                $"{delay} second timer trigger started on entity {ToPrettyString(uid):timer}");
        }

        var active = AddComp<ActiveTimerTriggerComponent>(uid);
        active.TimeRemaining = delay;
        active.User = user;
        active.BeepSound = beepSound;
        active.BeepInterval = beepInterval;
        active.TimeUntilBeep = initialBeepDelay ?? active.BeepInterval;

        var ev = new ActiveTimerTriggerEvent(uid, user);
        RaiseLocalEvent(uid, ref ev);

        if (TryComp<AppearanceComponent>(uid, out var appearance))
            _appearance.SetData(uid, TriggerVisuals.VisualState, TriggerVisualState.Primed, appearance);
    }

    public void TryDelay(EntityUid uid, float amount, ActiveTimerTriggerComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        comp.TimeRemaining += amount;
    }
}
