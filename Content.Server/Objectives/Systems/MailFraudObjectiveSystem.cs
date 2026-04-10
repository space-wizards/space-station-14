using Content.Server.Mind;
using Content.Server.Objectives.Components;
using Content.Shared.Delivery;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed partial class MailFraudObjectiveSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    private EntityQuery<MailFraudConditionComponent> _objQuery;


    public override void Initialize()
    {
        _objQuery = GetEntityQuery<MailFraudConditionComponent>();

        SubscribeLocalEvent<MailFraudConditionComponent, ObjectiveGetProgressEvent>(OnObjectiveGetProgress);
        SubscribeLocalEvent<DeliveryComponent, DeliveryOpenedEvent>(OnDeliveryOpened);
    }

    private void OnDeliveryOpened(Entity<DeliveryComponent> ent, ref DeliveryOpenedEvent args)
    {
        if (!ent.Comp.WasPenalized)
            return; //not fraud

        if (!_mind.TryGetMind(args.User, out _, out var mind))
            return;

        foreach (var objId in mind.Objectives)
        {
            if (_objQuery.TryGetComponent(objId, out var obj))
            {
                obj.MailFraudCommitted++;
                break;
            }
        }
    }

    private void OnObjectiveGetProgress(Entity<MailFraudConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(ent.Comp, _number.GetTarget(ent));
    }

    private float GetProgress(MailFraudConditionComponent comp, int target)
    {
        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        if (comp.MailFraudCommitted >= target)
            return 1f;

        return (float)comp.MailFraudCommitted / (float)target;
    }
}
