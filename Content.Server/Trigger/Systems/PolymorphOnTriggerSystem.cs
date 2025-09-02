using Content.Server.Polymorph.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Prototypes;

namespace Content.Server.Trigger.Systems;

public sealed partial class PolymorphOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;

    /// <summary>
    /// Need to do this so we don't get a collection enumeration error in physics by polymorphing
    /// an entity we're colliding with in case of TriggerOnCollide.
    /// Also makes sure other trigger effects don't activate in nullspace after we have polymorphed.
    /// </summary>
    private Queue<(EntityUid Uid, ProtoId<PolymorphPrototype> Polymorph)> _queuedPolymorphUpdates = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PolymorphOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<PolymorphOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        _queuedPolymorphUpdates.Enqueue((target.Value, ent.Comp.Polymorph));
        args.Handled = true;
    }

    public override void Update(float frametime)
    {
        while (_queuedPolymorphUpdates.TryDequeue(out var data))
        {
            if (TerminatingOrDeleted(data.Uid))
                continue;

            _polymorph.PolymorphEntity(data.Uid, data.Polymorph);
        }
    }
}
