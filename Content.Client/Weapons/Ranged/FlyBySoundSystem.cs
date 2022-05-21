using Content.Shared.Weapons.Ranged;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Client.Weapons.Ranged;

public sealed class FlyBySoundSystem : SharedFlyBySoundSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public const float FlyBySoundChance = 0.25f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FlyBySoundComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(EntityUid uid, FlyBySoundComponent component, StartCollideEvent args)
    {
        var attachedEnt = _player.LocalPlayer?.ControlledEntity;

        if (attachedEnt == null || args.OtherFixture.Body.Owner != attachedEnt) return;

        if (args.OurFixture.ID != FlyByFixture ||
            !_random.Prob(FlyBySoundChance)) return;

        SoundSystem.Play(Filter.Local(), component.Sound.GetSound(), uid, component.Sound.Params);
    }
}
