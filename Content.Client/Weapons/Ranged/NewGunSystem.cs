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

        Sawmill.Debug($"Handle state: setting shot count from {component.ShotCounter} to {state.ShotCounter}");
        component.NextFire = state.NextFire;
        component.ShotCounter = state.ShotCounter;
    }

    public override void Update(float frameTime)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

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
            if (gun.ShotCounter != 0)
                EntityManager.RaisePredictiveEvent(new RequestStopShootEvent() { Gun = gun.Owner });
            return;
        }

        if (gun.NextFire > Timing.CurTime)
            return;

        var mousePos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
        var oldShotCounter = gun.ShotCounter;

        Sawmill.Debug($"Sending shoot request tick {Timing.CurTick} / {Timing.CurTime}");

        EntityManager.RaisePredictiveEvent(new RequestShootEvent()
        {
            Coordinates = mousePos,
            Gun = gun.Owner,
        });
    }

    protected override void PlaySound(NewGunComponent gun, string? sound, int shots, EntityUid? user = null)
    {
        if (sound == null || user == null || !Timing.IsFirstTimePredicted) return;
        SoundSystem.Play(Filter.Local(), sound, gun.Owner);
    }
}
