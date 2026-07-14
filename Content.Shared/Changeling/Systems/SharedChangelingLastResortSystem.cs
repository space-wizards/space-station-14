using Content.Shared.Mind;
using Content.Shared.Actions;
using Content.Shared.Changeling.Components;
using Content.Shared.Gibbing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared.Changeling.Systems;

public abstract partial class SharedChangelingLastResortSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;

    [SubscribeLocalEvent]
    private void OnTakeOverMapInit(Entity<ChangelingSlugComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    [SubscribeLocalEvent]
    private void OnTakeOverShutdown(Entity<ChangelingSlugComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ActionEntity != null)
            _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    [SubscribeLocalEvent]
    private void OnLastResortAction(Entity<ChangelingLastResortAbilityComponent> ent,
        ref ChangelingLastResortActionEvent args)
    {
        if (args.Handled || !_mind.TryGetMind(args.Performer, out var mindId, out var mind))
            return;

        args.Handled = true;

        Audio.PlayPredicted(ent.Comp.Sound, args.Performer, args.Performer);

        if (!_net.IsServer)
            return; // Transfer Mind is unpredictable.

        var slug = PredictedSpawnAtPosition(ent.Comp.SlugPrototype, Transform(args.Performer).Coordinates);
        _mind.TransferTo(mindId, slug, mind: mind);
        _gibbing.Gib(args.Performer);
    }
}
