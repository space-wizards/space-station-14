using Content.Client.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Pulling;
using Content.Shared.Weapons.Ranged;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Client.Weapons.Ranged;

public sealed class NewGunSystem : SharedNewGunSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
        SubscribeLocalEvent<NewGunComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, NewGunComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NewGunComponentState state) return;

        component.NextFire = state.NextFire;
        component.ShotCounter = state.ShotCounter;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var entityNull = _player.LocalPlayer?.ControlledEntity;

        if (entityNull == null)
        {
            return;
        }

        var entity = entityNull.Value;
        var gun = GetGun(entity);

        if (gun == null)
        {
            return;
        }

        if (_inputSystem.CmdStates.GetState(EngineKeyFunctions.Use) != BoundKeyState.Down)
        {
            StopShooting(gun);
            return;
        }

        var mousePos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
        gun.ShootCoordinates = mousePos;
        var oldShotCounter = gun.ShotCounter;

        if (AttemptShoot(entity, gun))
        {
            if (IsPredictedShot(gun, oldShotCounter))
            {
                Sawmill.Debug($"Sending shoot request tick {Timing.CurTick} / {Timing.CurTime}");

                RaiseNetworkEvent(new RequestShootEvent()
                {
                    Coordinates = mousePos,
                    Gun = gun.Owner,
                });
            }
        }
    }

    private bool IsPredictedShot(NewGunComponent gun, int shots)
    {
        return shots == 0 || (Timing.IsFirstTimePredicted && Timing.InPrediction);
    }

    protected override void PlaySound(NewGunComponent gun, string? sound, int shots, EntityUid? user = null)
    {
        if (sound == null || user == null || !IsPredictedShot(gun, shots)) return;
        SoundSystem.Play(Filter.Local(), sound, gun.Owner);
    }
}
