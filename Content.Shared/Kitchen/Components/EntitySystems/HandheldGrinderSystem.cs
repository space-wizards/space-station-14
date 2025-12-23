using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen.EntitySystems;

internal sealed class HandheldGrinderSystem : EntitySystem
{
    [Dependency] private readonly SharedReagentGrinderSystem _reagentGrinder = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedStackSystem _stackSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructibleSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldGrinderComponent, UseInHandEvent>(OnInteractUsing);
        SubscribeLocalEvent<HandheldGrinderComponent, HandheldGrinderDoAfterEvent>(OnHandheldDoAfter);
    }

    private void OnInteractUsing(Entity<HandheldGrinderComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (_slots.GetItemOrNull(ent, ent.Comp.ItemSlotName) is not { } item)
            return;

        args.Handled = true;

        if (!CanGrinderBeUsed(ent, item, out var reason))
        {
            _popup.PopupClient(reason, ent, args.User);
            return;
        }

        if (_reagentGrinder.GetGrinderSolution(item, ent.Comp.Program) is null)
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out _, out _))
            return;

        if (_net.IsServer) // Cannot cancel predicted audio.
            ent.Comp.AudioStream = _audio.PlayPvs(ent.Comp.Sound, ent)?.Entity;

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.DoAfterDuration, new HandheldGrinderDoAfterEvent(), ent, ent, ent)
        {
            NeedHand = true,
            BreakOnDamage = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            BreakOnMove = true
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnHandheldDoAfter(Entity<HandheldGrinderComponent> ent, ref HandheldGrinderDoAfterEvent args)
    {
        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);

        if (args.Cancelled)
            return;

        if (_slots.GetItemOrNull(ent, ent.Comp.ItemSlotName) is not { } item)
            return;

        if (!CanGrinderBeUsed(ent, item, out var reason))
        {
            _popup.PopupClient(reason, ent, args.User);
            return;
        }

        if (_reagentGrinder.GetGrinderSolution(item, ent.Comp.Program) is not { } obtainedSolution)
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out var outputSolutionEnt, out var solution))
            return;

        if (TryComp<StackComponent>(item, out var stack))
        {
            _solution.TryMixAndOverflow(outputSolutionEnt.Value, obtainedSolution, solution.MaxVolume, out var overflow);
            if (overflow != null)
                _puddle.TrySpillAt(ent, overflow, out _);
            _stackSystem.ReduceCount((item, stack), 1);
        }
        else
        {
            _solution.TryMixAndOverflow(outputSolutionEnt.Value, obtainedSolution, solution.MaxVolume, out var overflow);
            if (overflow != null)
                _puddle.TrySpillAt(ent, overflow, out _);
            _destructibleSystem.DestroyEntity(item);
        }
    }

    private bool CanGrinderBeUsed(Entity<HandheldGrinderComponent> ent, EntityUid item, [NotNullWhen(false)] out string? reason)
    {
        reason = null;
        if (ent.Comp.Program == GrinderProgram.Grind && !_reagentGrinder.CanGrind(item))
        {
            reason = $"You cannot grind {item}!";
            return false;
        }

        if (ent.Comp.Program == GrinderProgram.Juice && !_reagentGrinder.CanJuice(item))
        {
            reason = $"You cannot juice {item}!";
            return false;
        }

        return true;
    }
}

[Serializable, NetSerializable]
public sealed partial class HandheldGrinderDoAfterEvent : SimpleDoAfterEvent;
