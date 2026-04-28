using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed class FlyBySoundSystem : SharedFlyBySoundSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FlyBySoundComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(Entity<FlyBySoundComponent> ent, ref StartCollideEvent args)
    {
        var attachedEnt = _player.LocalEntity;

        // If it's not our ent or we shot it.
        if (attachedEnt == null ||
            args.OtherEntity != attachedEnt ||
            TryComp<ProjectileComponent>(ent, out var projectile) &&
            projectile.Shooter == attachedEnt)
        {
            return;
        }

        if (args.OurFixtureId != FlyByFixture ||
            !_random.Prob(ent.Comp.Prob))
        {
            return;
        }

        // Play attached to our entity because the projectile may immediately delete or the likes.
        _audio.PlayPredicted(ent.Comp.Sound, attachedEnt.Value, attachedEnt.Value);
    }
}
