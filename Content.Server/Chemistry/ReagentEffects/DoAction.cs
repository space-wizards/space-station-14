using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.ReagentEffects;

/// <summary>
///     Forces someone to do a certain action, if they have it.
/// </summary>
public class DoAction : ReagentEffect
{
    [DataField("action", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<ActionPrototype>))]
    public string Action = default!;

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.EntityManager.TryGetComponent(args.SolutionEntity, out SharedActionsComponent? actions))
        {
            if (!IoCManager.Resolve<IPrototypeManager>().TryIndex<ActionPrototype>(Action, out var proto))
                return;

            if (actions.IsGranted(proto.ActionType))
            {
                var attempt = new ActionAttempt(proto);
                attempt.DoInstantAction(args.SolutionEntity);
            }
        }
    }
}
