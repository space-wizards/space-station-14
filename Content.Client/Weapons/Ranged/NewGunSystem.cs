using Content.Client.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Weapons.Ranged;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.Player;

namespace Content.Client.Weapons.Ranged;

public sealed class NewGunSystem : SharedNewGunSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;

    /// <summary>
    /// Due to the fact you may be re-running already predicted ticks and fire for the first time
    /// We need to track if we need to send the server our shot.
    /// </summary>
    private bool _firstShot = true;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
        SubscribeLocalEvent<NewGunComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, NewGunComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NewGunComponentState state) return;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var entityNull = _player.LocalPlayer?.ControlledEntity;

        if (entityNull == null)
        {
            _firstShot = true;
            return;
        }

        var entity = entityNull.Value;
        var gun = GetGun(entity);

        if (gun == null)
        {
            _firstShot = true;
            return;
        }

        if (_inputSystem.CmdStates.GetState(EngineKeyFunctions.Use) != BoundKeyState.Down)
        {
            _firstShot = true;
            StopShooting(gun);
            return;
        }

        var mousePos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
        gun.ShootCoordinates = mousePos;

        if (AttemptShoot(entity, gun))
        {
            if (PredictedShoot)
            {
                Sawmill.Debug($"Sending shoot request tick {Timing.CurTick} / {Timing.CurTime}");

                RaiseNetworkEvent(new RequestShootEvent()
                {
                    Coordinates = mousePos,
                    Gun = gun.Owner,
                });
            }

            _firstShot = false;
        }
    }

    private bool PredictedShoot => (_firstShot || Timing.IsFirstTimePredicted) && Timing.InPrediction;

    protected override void PlaySound(EntityUid gun, string? sound, EntityUid? user = null)
    {
        if (sound == null || user == null || !PredictedShoot) return;
        SoundSystem.Play(Filter.Local(), sound, gun);
    }
}
