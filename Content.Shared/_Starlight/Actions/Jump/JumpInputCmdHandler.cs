using System;
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Charges.Systems;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._Starlight.Actions.Jump;

public sealed class JumpInputCmdHandler(SharedActionsSystem actions, SharedChargesSystem charges, IGameTiming timing) : InputCmdHandler
{
    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if (session?.AttachedEntity is not { } uid
            || (entManager.TryGetComponent<PilotComponent>(uid, out var pilot) && pilot.Console != null)) return false;

        var coordinates = message switch
        {
            ClientFullInputCmdMessage clientInput => clientInput.Coordinates,
            FullInputCmdMessage fullInput => entManager.GetCoordinates(fullInput.Coordinates),
            _ => default
        };
        if (coordinates == default)
            return false;

        foreach (var (Id, Comp) in actions.GetActions(uid))
        {
            if (entManager.HasComponent<ActivationOnJumpComponent>(Id)
                 && entManager.TryGetComponent<WorldTargetActionComponent>(Id, out var worldTarget)
                 && worldTarget.Event is not null)
            {
                if (!actions.IsCooldownActive(Comp, timing.CurTime)
                 && !charges.IsEmpty((Id, null)))
                {
                    var ev = new RequestPerformActionEvent(entManager.GetNetEntity(uid), entManager.GetNetCoordinates(coordinates));
                    var validateEv = new ActionValidateEvent()
                    {
                        Input = ev,
                        User = uid,
                        Provider = uid
                    };
                    entManager.EventBus.RaiseLocalEvent(Id, ref validateEv);
                    if (validateEv.Invalid)
                        return false;

                    var @event = worldTarget.Event;
                    @event.Target = coordinates;
                    actions.PerformAction((uid, null), (Id, Comp), @event);
                }
                return false;
            }
        }
        return false;
    }
}