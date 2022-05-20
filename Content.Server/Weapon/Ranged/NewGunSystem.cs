using Content.Shared.Weapons.Ranged;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Input;
using Robust.Shared.Player;

namespace Content.Server.Weapon.Ranged;

public sealed class NewGunSystem : SharedNewGunSystem
{
    [Dependency] private readonly InputSystem _inputSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RequestShootEvent>(OnShootRequest);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var session in Filter.GetAllPlayers())
        {
            if (session.AttachedEntity == null) continue;

            var entity = session.AttachedEntity.Value;
            var pSession = (IPlayerSession) session;
            var gun = GetGun(entity);

            if (gun == null) continue;

            if (_inputSystem.GetInputStates(pSession).GetState(EngineKeyFunctions.Use) != BoundKeyState.Down)
            {
                StopShooting(gun);
                continue;
            }

            AttemptShoot(entity, gun);
        }
    }

    private void OnShootRequest(RequestShootEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null ||
            !HasComp<NewGunComponent>(msg.Gun)) return;

        var gun = GetGun(user.Value);

        if (gun?.Owner != msg.Gun) return;

        gun.ShootCoordinates = msg.Coordinates;
        Sawmill.Debug($"Set shoot coordinates to {gun.ShootCoordinates}");
    }


    protected override void PlaySound(EntityUid gun, string? sound, EntityUid? user = null)
    {
        if (sound == null) return;

        SoundSystem.Play(Filter.Pvs(gun).RemoveWhereAttachedEntity(e => e == user), sound, gun);
    }
}
