using Content.Server.Mind;
using Content.Server.Objectives.Components;
using Content.Shared.Delivery;
using Content.Shared.FingerprintReader;

namespace Content.Server.Objectives.Systems;

public sealed partial class MailFraudObjectiveSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly FingerprintReaderSystem _fingerprintReader = default!;
    [Dependency] private readonly CounterConditionSystem _counterCondition = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DeliveryComponent, DeliveryOpenedEvent>(OnDeliveryOpened);
    }

    private void OnDeliveryOpened(Entity<DeliveryComponent> ent, ref DeliveryOpenedEvent args)
    {
        if (!ent.Comp.WasPenalized)
            return; //not fraud

        if (_fingerprintReader.IsAllowed(ent.Owner, args.User, out var _, showPopup: false, checkGloves: false))
            return; //cutting open your own letter

        if (!_mind.TryGetMind(args.User, out _, out var mind))
            return;

        foreach (var obj in mind.Objectives)
        {
            if (HasComp<MailFraudConditionComponent>(obj))
            {
                _counterCondition.IncreaseCount(obj);
            }
        }
    }
}
