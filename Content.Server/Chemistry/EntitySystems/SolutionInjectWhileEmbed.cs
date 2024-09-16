using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace Content.Server.Chemistry.EntitySystems;

/// <summary>
/// System for handling the different inheritors of <see cref="BaseSolutionInjectOnEventComponent"/>.
/// Subscribes to relevent events and performs solution injections when they are raised.
/// </summary>
public sealed class SolutionInjectWhileEmbedSystem : EntitySystem
{
	[Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

	public override void Update(float frameTime)
	{
        var query = EntityQueryEnumerator<SolutionInjectWhileEmbedComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_gameTiming.CurTime < component.NextUpdate)
                continue;
            if(component.EmbeddedIntoUid == null)
                continue;

			RaiseLocalEvent();
            component.NextUpdate += component.UpdateInterval;
		}
	}


    private void DoInjection(Entity<SolutionInjectWhileEmbedComponent> injectorEntity, EntityUid target, EntityUid? source = null)
    {
        TryInjectTargets(injectorEntity, [target], source);
    }

    /// <summary>
    /// Filters <paramref name="targets"/> for valid targets and tries to inject a portion of <see cref="BaseSolutionInjectOnEventComponent.Solution"/> into
    /// each valid target's bloodstream.
    /// </summary>
    /// <remarks>
    /// Targets are invalid if any of the following are true:
    /// <list type="bullet">
    ///     <item>The target does not have a bloodstream.</item>
    ///     <item><see cref="BaseSolutionInjectOnEventComponent.PierceArmor"/> is false and the target is wearing a hardsuit.</item>
    ///     <item><see cref="BaseSolutionInjectOnEventComponent.BlockSlots"/> is not NONE and the target has an item equipped in any of the specified slots.</item>
    /// </list>
    /// </remarks>
    /// <returns>true if at least one target was successfully injected, otherwise false</returns>
    private bool TryInjectTargets(Entity<SolutionInjectWhileEmbedComponent> injector, IReadOnlyList<EntityUid> targets, EntityUid? source = null)
    {
        // Make sure we have at least one target
        if (targets.Count == 0)
            return false;

        // Get the solution to inject
        if (!_solutionContainer.TryGetSolution(injector.Owner, injector.Comp.Solution, out var injectorSolution))
            return false;

        // Build a list of bloodstreams to inject into
        var targetBloodstreams = new ValueList<Entity<BloodstreamComponent>>();
        foreach (var target in targets)
        {
            if (Deleted(target))
                continue;

            // Yuck, this is way to hardcodey for my tastes
            // TODO blocking injection with a hardsuit should probably done with a cancellable event or something
            if (!injector.Comp.PierceArmor && _inventory.TryGetSlotEntity(target, "outerClothing", out var suit) && _tag.HasTag(suit.Value, "Hardsuit"))
            {
                // Only show popup to attacker
                if (source != null)
                    _popup.PopupEntity(Loc.GetString(injector.Comp.BlockedByHardsuitPopupMessage, ("weapon", injector.Owner), ("target", target)), target, source.Value, PopupType.SmallCaution);

                continue;
            }

            // Check if the target has anything equipped in a slot that would block injection
            if (injector.Comp.BlockSlots != SlotFlags.NONE)
            {
                var blocked = false;
                var containerEnumerator = _inventory.GetSlotEnumerator(target, injector.Comp.BlockSlots);
                while (containerEnumerator.MoveNext(out var container))
                {
                    if (container.ContainedEntity != null)
                    {
                        blocked = true;
                        break;
                    }
                }
                if (blocked)
                    continue;
            }

            // Make sure the target has a bloodstream
            if (!TryComp<BloodstreamComponent>(target, out var bloodstream))
                continue;


            // Checks passed; add this target's bloodstream to the list
            targetBloodstreams.Add((target, bloodstream));
        }

        // Make sure we got at least one bloodstream
        if (targetBloodstreams.Count == 0)
            return false;

        // Extract total needed solution from the injector
        var removedSolution = _solutionContainer.SplitSolution(injectorSolution.Value, injector.Comp.TransferAmount * targetBloodstreams.Count);
        // Adjust solution amount based on transfer efficiency
        var solutionToInject = removedSolution.SplitSolution(removedSolution.Volume * injector.Comp.TransferEfficiency);
        // Calculate how much of the adjusted solution each target will get
        var volumePerBloodstream = solutionToInject.Volume * (1f / targetBloodstreams.Count);

        var anySuccess = false;
        foreach (var targetBloodstream in targetBloodstreams)
        {
            // Take our portion of the adjusted solution for this target
            var individualInjection = solutionToInject.SplitSolution(volumePerBloodstream);
            // Inject our portion into the target's bloodstream
            if (_bloodstream.TryAddToChemicals(targetBloodstream.Owner, individualInjection, targetBloodstream.Comp))
                anySuccess = true;
        }

        // Huzzah!
        return anySuccess;
    }
}
