using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Fluids;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen.EntitySystems;

internal sealed class HandheldGrinderSystem : EntitySystem
{
    [Dependency] private readonly SharedReagentGrinderSystem _reagentGrinder = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedStackSystem _stackSystem = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructibleSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldGrinderComponent, EntRemovedFromContainerMessage>(OnGrinderRemoved);
        SubscribeLocalEvent<HandheldGrinderComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<HandheldGrinderComponent, HandheldGrinderDoAfterEvent>(OnHandheldDoAfter);
    }

    // prevent the infamous UdderSystem debug assert, see https://github.com/space-wizards/space-station-14/pull/35314
    // TODO: find a better solution than copy pasting this into every shared system that caches solution entities
    private void OnGrinderRemoved(Entity<HandheldGrinderComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        // Make sure the removed entity was our contained solution and set it to null
        if (args.Entity != entity.Comp.GrinderSolution?.Owner)
            return;

        entity.Comp.GrinderSolution = null;
    }

    private void OnInteractUsing(Entity<HandheldGrinderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var item = args.Used;

        if (!CanGrinderBeUsed(ent, item, out var reason))
        {
            _popup.PopupClient(reason, ent, args.User);
            return;
        }

        if (_reagentGrinder.GetGrinderSolution(item, ent.Comp.Program) is null)
            return;

        if (!_solution.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.GrinderSolution))
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.DoAfterDuration, new HandheldGrinderDoAfterEvent(), ent, ent, item)
        {
            NeedHand = true,
            BreakOnDamage = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            BreakOnMove = true
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            ent.Comp.AudioStream = _audio.PlayPredicted(ent.Comp.Sound, ent, args.User)?.Entity ?? ent.Comp.AudioStream;
    }

    private void OnHandheldDoAfter(Entity<HandheldGrinderComponent> ent, ref HandheldGrinderDoAfterEvent args)
    {
        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);

        if (args.Cancelled)
            return;

        if (args.Used is not { } item)
            return;

        if (!CanGrinderBeUsed(ent, item, out var reason))
        {
            _popup.PopupClient(reason, ent, args.User);
            return;
        }

        if (_reagentGrinder.GetGrinderSolution(item, ent.Comp.Program) is not { } obtainedSolution)
            return;

        if (!_solution.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.GrinderSolution, out var solution))
            return;

        _solution.TryMixAndOverflow(ent.Comp.GrinderSolution.Value, obtainedSolution, solution.MaxVolume, out var overflow);

        if (overflow != null)
            _puddle.TrySpillAt(ent, overflow, out _);

        if (TryComp<StackComponent>(item, out var stack))
            _stackSystem.ReduceCount((item, stack), 1);
        else
            _destructibleSystem.DestroyEntity(item);

        _popup.PopupClient(Loc.GetString(ent.Comp.FinishedPopup, ("item", item)), ent, args.User);
    }

    /// <summary>
    /// Checks whether the respective handheld grinder can currently be used.
    /// </summary>
    /// <param name="ent">The grinder entity.</param>
    /// <param name="item">The item it is being used on.</param>
    /// <param name="reason">Reason the grinder cannot be used. Null if the function returns true.</param>
    /// <returns>True if the grinder can be used, otherwise false.</returns>
    public bool CanGrinderBeUsed(Entity<HandheldGrinderComponent> ent, EntityUid item, [NotNullWhen(false)] out string? reason)
    {
        reason = null;
        if (ent.Comp.Program == GrinderProgram.Grind && !_reagentGrinder.CanGrind(item))
        {
            reason = Loc.GetString("handheld-grinder-cannot-grind", ("item", item));
            return false;
        }

        if (ent.Comp.Program == GrinderProgram.Juice && !_reagentGrinder.CanJuice(item))
        {
            reason = Loc.GetString("handheld-grinder-cannot-juice", ("item", item));
            return false;
        }

        return true;
    }
}

/// <summary>
/// DoAfter used to indicate the handheld grinder is in use.
/// After it ends, the GrinderProgram from <see cref="HandheldGrinderComponent"/> is used on the contents.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class HandheldGrinderDoAfterEvent : SimpleDoAfterEvent;
