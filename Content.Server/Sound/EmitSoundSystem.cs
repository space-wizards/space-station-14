using Content.Shared.Sound;
using Content.Shared.Sound.Components;
using Robust.Shared.Timing;
using Robust.Shared.Network;

namespace Content.Server.Sound;

public sealed class EmitSoundSystem : SharedEmitSoundSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<SpamEmitSoundComponent>();

        while (query.MoveNext(out var uid, out var soundSpammer))
        {
            if (!soundSpammer.Enabled)
                continue;

            if (_timing.CurTime >= soundSpammer.NextSound)
            {
                if (soundSpammer.PopUp != null)
                    Popup.PopupEntity(Loc.GetString(soundSpammer.PopUp), uid);
                TryEmitSound(uid, soundSpammer, predict: false);

                SpamEmitSoundReset((uid, soundSpammer));
            }
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpamEmitSoundComponent, MapInitEvent>(HandleSpamEmitSoundMapInit);
    }

    private void HandleSpamEmitSoundMapInit(Entity<SpamEmitSoundComponent> entity, ref MapInitEvent args)
    {
        SpamEmitSoundReset(entity);

        // Prewarm so multiple entities have more variation.
        entity.Comp.NextSound -= Random.Next(entity.Comp.MaxInterval);
        Dirty(entity);
    }

    private void SpamEmitSoundReset(Entity<SpamEmitSoundComponent> entity)
    {
        if (_net.IsClient)
            return;

        entity.Comp.NextSound = _timing.CurTime + ((entity.Comp.MinInterval < entity.Comp.MaxInterval)
            ? Random.Next(entity.Comp.MinInterval, entity.Comp.MaxInterval)
            : entity.Comp.MaxInterval);

        Dirty(entity);
    }

    public override void SetEnabled(Entity<SpamEmitSoundComponent?> entity, bool enabled)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (entity.Comp.Enabled == enabled)
            return;

        entity.Comp.Enabled = enabled;

        if (enabled)
            SpamEmitSoundReset((entity, entity.Comp));
    }
}
