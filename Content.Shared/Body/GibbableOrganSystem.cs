using Content.Shared.Gibbing;

namespace Content.Shared.Body;

public sealed class GibbableOrganSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GibbableOrganComponent, BodyRelayedEvent<BeingGibbedEvent>>(OnBeingGibbed);
    }

    private void OnBeingGibbed(Entity<GibbableOrganComponent> ent, ref BodyRelayedEvent<BeingGibbedEvent> args)
    {
        args.Args.Giblets.Add(ent);
    }
}
