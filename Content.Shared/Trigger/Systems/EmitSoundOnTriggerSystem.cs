using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared.Trigger.Systems;

public sealed class EmitSoundOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmitSoundOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<EmitSoundOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        args.Handled |= TryEmitSound(ent, target.Value, args.User);
    }

    private bool TryEmitSound(Entity<EmitSoundOnTriggerComponent> ent, EntityUid target, EntityUid? user = null)
    {
        if (ent.Comp.Sound == null)
            return false;

        if (ent.Comp.Positional)
        {
            var coords = Transform(target).Coordinates;
            if (ent.Comp.Predicted)
                _audio.PlayPredicted(ent.Comp.Sound, coords, user);
            else if (_netMan.IsServer)
                _audio.PlayPvs(ent.Comp.Sound, coords);
        }
        else
        {
            if (ent.Comp.Predicted)
                _audio.PlayPredicted(ent.Comp.Sound, target, user);
            else if (_netMan.IsServer)
                _audio.PlayPvs(ent.Comp.Sound, target);
        }

        return true;
    }
}
