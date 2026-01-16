using Content.Shared.Gibbing;

namespace Content.Shared.Body;

public sealed class GibbableOrganSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Body.GibbableOrganComponent, BodyRelayedEvent<BeingGibbedEvent>>(OnBeingGibbed);
    }

    private void OnBeingGibbed(Entity<Body.GibbableOrganComponent> ent, ref BodyRelayedEvent<BeingGibbedEvent> args)
    {
        args.Args.Giblets.Add(ent);
    }
}
