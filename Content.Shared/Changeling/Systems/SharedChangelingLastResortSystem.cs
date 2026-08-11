using Content.Shared.Mind;
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
    [Dependency] private DestructionResistanceSystem _destructionResistance = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;

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

        _destructionResistance.SetEnabled(args.Performer, false);

        _gibbing.Gib(args.Performer);
    }
}
