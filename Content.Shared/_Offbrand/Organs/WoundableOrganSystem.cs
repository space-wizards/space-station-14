using Content.Shared._Offbrand.Input;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body;
using Content.Shared.Hands;

namespace Content.Shared._Offbrand.Organs;

public sealed partial class WoundableOrganSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<WoundableOrganWeightsEvent>>(OnGetWeights);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<GetWoundsWithSpaceEvent>>(UnwrapRelay);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<GetPainEvent>>(UnwrapRelay);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<HealWoundsEvent>>(UnwrapRelay);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<GetBleedLevelEvent>>(UnwrapRelay);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<ClampWoundsEvent>>(UnwrapRelay);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<BeforeEquippingHandEvent>>(UnwrapRelay);
    }

    private void OnGetWeights(Entity<WoundableOrganComponent> ent, ref BodyRelayedEvent<WoundableOrganWeightsEvent> args)
    {
        if (ent.Comp.Weights.TryGetValue(args.Args.TargetZone, out var weight))
            args.Args.Weights[ent] = weight;
    }

    public Dictionary<Entity<WoundableOrganComponent>, float> GetWoundableOrgans(EntityUid body, OffbrandTargetZone targetZone)
    {
        var organs = new WoundableOrganWeightsEvent(new(), targetZone);
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
