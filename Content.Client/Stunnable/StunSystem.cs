using Content.Shared.CombatMode;
using Content.Shared.Interaction;
using Content.Shared.Stunnable;
using Robust.Client.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client.Stunnable;

public sealed class StunSystem : SharedStunSystem
{
    // TODO: Clientside prediction
    // DoAfter mis-predicts on client hard when in shared so it's gonna need it's own special system because it hates me.

    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly InputSystem _input = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .BindAfter(EngineKeyFunctions.UseSecondary, new PointerInputCmdHandler(OnUseSecondary, true, true), typeof(SharedInteractionSystem))
            .Register<StunSystem>();
    }

    private bool OnUseSecondary(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.Session?.AttachedEntity is not {Valid: true} uid)
            return false;

        if (_input.Predicted)
            return false;

        if (args.EntityUid != uid || !HasComp<KnockedDownComponent>(uid) || !_combat.IsInCombatMode(uid))
            return false;

        RaisePredictiveEvent(new ForceStandUpEvent());
        return true;
    }
}

