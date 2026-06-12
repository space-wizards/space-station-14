using Content.Shared.Body;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Robust.Shared.Containers;

namespace Content.Shared._Offbrand.Organs;

public sealed partial class OffbrandOrganSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganComponent, AccessibleOverrideEvent>(OnAccessibleOverride);
        SubscribeLocalEvent<OrganComponent, BeforeGettingEquippedHandEvent>(OnBeforeGettingEquippedHand);
    }

    private void OnBeforeGettingEquippedHand(Entity<OrganComponent> ent, ref BeforeGettingEquippedHandEvent args)
    {
        if (ent.Comp.Body is null)
            return;

        args.Cancelled = true;
    }

    private void OnAccessibleOverride(Entity<OrganComponent> ent, ref AccessibleOverrideEvent args)
    {
        if (args.Handled || ent.Comp.Body is not { } body)
            return;

        if (!_container.IsInSameOrParentContainer(args.User, body, out _, out _))
            return;

        args.Accessible = true;
        args.Handled = true;
    }
}
