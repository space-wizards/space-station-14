using Content.Client.CombatMode;
using Content.Client.Verbs;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Interactable;

/// <summary>
/// This handles the logic whether an interaction was done by holding or pressing the Use key (Default: Left Click)
/// </summary>
public sealed class InteractionSystem : SharedInteractionSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly CombatModeSystem _combat = default!;
    [Dependency] private readonly InputSystem _input = default!;
    [Dependency] private readonly VerbSystem _verb = default!;

    // This timespan will be added onto the time of a buttonpress to determine when it turns into a held button press.
    private readonly TimeSpan RequiredButtonHeldTime = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// screen pos where the mouse down began for the drag
    /// </summary>
    private EntityCoordinates _mouseDownScreenPos;
    private EntityUid _target;
    private TimeSpan _threshold;
    private bool _pressed;

    public override void Initialize()
    {
        base.Initialize();
        // UpdatesOutsidePrediction = true;
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.Use,
            new PointerInputCmdHandler(OnUse, false, true))
            .Register<InteractionSystem>();
    }

    private bool OnUse(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (_input.Predicted)
            return false;

        switch (args.State)
        {
            case BoundKeyState.Down:
                _threshold = _timing.CurTime + RequiredButtonHeldTime;
                _target = args.EntityUid;
                _mouseDownScreenPos = args.Coordinates;
                _pressed = true;
                // If the player is in combat or has no utility verbs, do not check for held inputs.
                if (NoVerbsOrInCombat())
                    RaiseNonUtilityEvent(); // This will also reset _pressed to false, so no double dipping.

                return true;
            case BoundKeyState.Up:
                if (!_pressed)
                    return false;

                RaiseNonUtilityEvent();
                return true;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private bool NoVerbsOrInCombat()
    {
        if (_combat.IsInCombatMode(_player.LocalEntity) || _player.LocalEntity == null)
            return true;

        var verb = _verb.GetLocalVerbs(_target, _player.LocalEntity.Value, typeof(UtilityVerb));
        return verb.Count == 0;
    }

    private void RaiseNonUtilityEvent()
    {
        if (_player.LocalEntity == null)
            return;

        _pressed = false;
        var ev = new InteractionRequestEvent(
            GetNetEntity(_player.LocalEntity.Value),
            GetNetEntity(_target),
            GetNetEntity(_mouseDownScreenPos.EntityId),
            _mouseDownScreenPos.Position,
            false);

        RaisePredictiveEvent(ev);
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        // When the timer reaches the threshold for holding the input, it'll automatically fire off the event.
        if (!_pressed || !_timing.IsFirstTimePredicted || _timing.CurTime < _threshold || _player.LocalEntity == null)
            return;

        var ev = new InteractionRequestEvent(
            GetNetEntity(_player.LocalEntity.Value),
            GetNetEntity(_target),
            GetNetEntity(_mouseDownScreenPos.EntityId),
            _mouseDownScreenPos.Position,
            true);

        RaisePredictiveEvent(ev);
        _pressed = false;
    }
}

