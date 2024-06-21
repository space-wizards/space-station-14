using Content.Server.Polymorph.Components;
using Content.Shared.Polymorph;
using Content.Shared.Projectiles;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.Polymorph.Systems;

public partial class PolymorphSystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    /// <summary>
    /// Need to do this so we don't get a collection enumeration error in physics by polymorphing
    /// an entity we're colliding with
    /// </summary>
    private Queue<PolymorphQueuedData> _queuedPolymorphUpdates = new();

    private void InitializeCollide()
    {
        SubscribeLocalEvent<PolymorphOnCollideComponent, StartCollideEvent>(OnPolymorphCollide);
    }

    public void UpdateCollide()
    {
        while (_queuedPolymorphUpdates.TryDequeue(out var data))
        {
            if (Deleted(data.Ent))
                continue;

            var ent = PolymorphEntity(data.Ent, data.Polymorph);
            if (ent != null)
                _audio.PlayPvs(data.Sound, ent.Value);
        }
    }

    private void OnPolymorphCollide(EntityUid uid, PolymorphOnCollideComponent component, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != SharedProjectileSystem.ProjectileFixture)
            return;

        var other = args.OtherEntity;
        if (_whitelistSystem.IsWhitelistFail(component.Whitelist, other) ||
            _whitelistSystem.IsBlacklistPass(component.Blacklist, other))
            return;

        _queuedPolymorphUpdates.Enqueue(new (other, component.Sound, component.Polymorph));
    }
}

public struct PolymorphQueuedData
{
    public EntityUid Ent;
    public SoundSpecifier Sound;
    public ProtoId<PolymorphPrototype> Polymorph;

    public PolymorphQueuedData(EntityUid ent, SoundSpecifier sound, ProtoId<PolymorphPrototype> polymorph)
    {
        Ent = ent;
        Sound = sound;
        Polymorph = polymorph;
    }
}
