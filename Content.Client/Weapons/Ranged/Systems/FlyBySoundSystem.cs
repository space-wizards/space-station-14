using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class FlyBySoundSystem : SharedFlyBySoundSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FlyBySoundComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(Entity<FlyBySoundComponent> ent, ref StartCollideEvent args)
    {
        if (_player.LocalEntity is not { } attachedEnt|| args.OtherEntity != attachedEnt)
            return;

        if (args.OurFixtureId != FlyByFixture)
            return;

        // If it's not our ent or we shot it.
        if (TryComp<ProjectileComponent>(ent, out var projectile) && projectile.Shooter == attachedEnt)
            return;

        if (!_random.Prob(ent.Comp.Prob))
            return;

        // Play attached to our entity because the projectile may immediately delete or the likes.
        _audio.PlayEntity(ent.Comp.Sound, attachedEnt, attachedEnt);
    }
}
