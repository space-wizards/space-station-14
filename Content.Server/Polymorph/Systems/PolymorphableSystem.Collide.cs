using Content.Server.Polymorph.Components;
using Content.Shared.Projectiles;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Server.Polymorph.Systems;

public partial class PolymorphableSystem
{
    // Need to do this so we don't get a collection enumeration error in physics by polymorphing
    // an entity we're colliding with
    private Queue<PolymorphQueuedData> _queuedPolymorphUpdates = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        while (_queuedPolymorphUpdates.TryDequeue(out var data))
        {
            if (Deleted(data.Ent))
                continue;

            var ent = PolymorphEntity(data.Ent, data.Polymorph);
            if (ent != null)
            {
                SoundSystem.Play(data.Sound.GetSound(), Filter.Pvs(ent.Value, entityManager: EntityManager),
                    ent.Value, data.Sound.Params);
            }
        }
    }

    private void InitializeCollide()
    {
        SubscribeLocalEvent<PolymorphOnCollideComponent, StartCollideEvent>(OnPolymorphCollide);
    }

    private void OnPolymorphCollide(EntityUid uid, PolymorphOnCollideComponent component, ref StartCollideEvent args)
    {
        if (args.OurFixture.ID != SharedProjectileSystem.ProjectileFixture)
            return;

        var other = args.OtherFixture.Body.Owner;
        if (!component.Whitelist.IsValid(other)
            || component.Blacklist != null && component.Blacklist.IsValid(other))
            return;

        _queuedPolymorphUpdates.Enqueue(new (other, component.Sound, component.Polymorph));
    }
}

struct PolymorphQueuedData
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
