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
    [Dependency] private readonly CombatModeSystem _combatMode = default!;

    private bool _hasShotOnce = false;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
        SubscribeLocalEvent<NewGunComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, NewGunComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NewGunComponentState state) return;

        if (state.NextFire < component.NextFire) return;

        component.NextFire = state.NextFire;
    }

    private void StopShooting(NewGunComponent? gun = null)
    {
        _hasShotOnce = false;

        if (gun != null)
        {
            gun.ShotCounter = 0;
            gun.AttemptedShotLastTick = false;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var entity = _player.LocalPlayer?.ControlledEntity;
        if (!EntityManager.TryGetComponent(entity, out SharedHandsComponent? hands) ||
            hands.ActiveHandEntity is not { } held)
        {
            StopShooting();
            return;
        }

        if (!EntityManager.TryGetComponent(held, out NewGunComponent? gun))
        {
            StopShooting(gun);
            return;
        }

        var state = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
        if (!_combatMode.IsInCombatMode() || state != BoundKeyState.Down)
        {
            StopShooting(gun);
            return;
        }

        if (!_hasShotOnce)
        {
            gun.NextFire = Timing.CurTime;
        }

        var mousePos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);

        if (TryShoot(entity.Value, gun, mousePos, out var shots))
        {
            if (PredictedShoot)
            {
                RaiseNetworkEvent(new RequestShootEvent()
                {
                    FirstShot = !_hasShotOnce,
                    Gun = gun.Owner,
                    Coordinates = mousePos,
                    Shots = shots,
                });

                Sawmill.Debug($"Sending shoot request at {Timing.CurTime} / tick {Timing.CurTick}");
            }

            _hasShotOnce = true;
        }
    }

    private bool PredictedShoot => !_hasShotOnce || Timing.IsFirstTimePredicted;

    protected override void PlaySound(EntityUid gun, string? sound, EntityUid? user = null)
    {
        if (sound == null || user == null || !PredictedShoot) return;
        SoundSystem.Play(Filter.Local(), sound, gun);
    }
}
