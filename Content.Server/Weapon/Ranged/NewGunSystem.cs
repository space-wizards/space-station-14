using Content.Shared.Weapons.Ranged;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Weapon.Ranged;

public sealed class NewGunSystem : SharedNewGunSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RequestShootEvent>(OnShootRequest);
    }

    private void OnShootRequest(RequestShootEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null ||
            !TryComp<NewGunComponent>(msg.Gun, out var gun)) return;

        if (!TryShoot(user.Value, gun, msg.Coordinates, out var shots)) return;

        Sawmill.Debug($"Shot at tick {Timing.CurTick} / {Timing.CurTime}");
    }

    protected override void PlaySound(EntityUid gun, string? sound, EntityUid? user = null)
    {
        if (sound == null) return;

        SoundSystem.Play(Filter.Pvs(gun).RemoveWhereAttachedEntity(e => e == user), sound, gun);
    }
}
