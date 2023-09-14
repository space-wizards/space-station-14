using Content.Server.Polymorph.Components;
using Content.Shared.Projectiles;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Events;

namespace Content.Server.Polymorph.Systems;

public partial class PolymorphSystem
{
    // Need to do this so we don't get a collection enumeration error in physics by polymorphing
    // an entity we're colliding with
    private Queue<PolymorphQueuedData> _queuedPolymorphUpdates = new();

    public void UpdateCollide()
    {
        while (_queuedPolymorphUpdates.TryDequeue(out var data))
        {
            if (Deleted(data.Ent))
                continue;

            var ent = PolymorphEntity(data.Ent, data.Polymorph);
            if (ent != null)
            {
                _audio.PlayPvs(data.Sound, ent.Value);
            }
        }
    }

    private void InitializeCollide()
    {
        SubscribeLocalEvent<PolymorphOnCollideComponent, StartCollideEvent>(OnPolymorphCollide);
    }

    private void OnPolymorphCollide(EntityUid uid, PolymorphOnCollideComponent component, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != SharedProjectileSystem.ProjectileFixture)
            return;

        var other = args.OtherEntity;
        if (!component.Whitelist.IsValid(other)
            || component.Blacklist != null && component.Blacklist.IsValid(other))
            return;

        _queuedPolymorphUpdates.Enqueue(new (other, component.Sound, component.Polymorph));
    }
}

public struct PolymorphQueuedData
{
    public EntityUid Ent;
    public SoundSpecifier Sound;
    public string Polymorph;

    public PolymorphQueuedData(EntityUid ent, SoundSpecifier sound, string polymorph)
    {
        Ent = ent;
        Sound = sound;
        Polymorph = polymorph;
    }
}
