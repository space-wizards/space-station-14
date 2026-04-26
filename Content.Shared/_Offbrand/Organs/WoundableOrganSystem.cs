using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body;

namespace Content.Shared._Offbrand.Organs;

public sealed class WoundableOrganSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<WoundableOrganWeightsEvent>>(OnGetWeights);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<WoundGetDamageEvent>>(UnwrapRelay);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<GetWoundsWithSpaceEvent>>(UnwrapRelay);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<GetPainEvent>>(UnwrapRelay);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<HealWoundsEvent>>(UnwrapRelay);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<GetBleedLevelEvent>>(UnwrapRelay);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<ClampWoundsEvent>>(UnwrapRelay);
    }

    private void OnGetWeights(Entity<WoundableOrganComponent> ent, ref BodyRelayedEvent<WoundableOrganWeightsEvent> args)
    {
        args.Args.Weights[ent] = ent.Comp.Weight;
    }

    public Dictionary<Entity<WoundableOrganComponent>, float> GetWoundableOrgans(EntityUid body)
    {
        var organs = new WoundableOrganWeightsEvent(new());
        RaiseLocalEvent(body, ref organs);
        return organs.Weights;
    }

    private void UnwrapRelay<TEvent>(Entity<WoundableOrganComponent> ent, ref BodyRelayedEvent<TEvent> args) where TEvent : struct
    {
        var evt = args.Args;
        RaiseLocalEvent(ent, ref evt);
        args.Args = evt;
    }
}
