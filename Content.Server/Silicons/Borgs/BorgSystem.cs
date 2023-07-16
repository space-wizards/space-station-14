using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Interaction;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Server.Silicons.Borgs;

/// <inheritdoc/>
public sealed class BorgSystem : SharedBorgSystem
{
    [Dependency] private readonly BodySystem _bobby = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgChassisComponent, AfterInteractUsingEvent>(OnChassisInteractUsing);
    }

    private void OnChassisInteractUsing(EntityUid uid, BorgChassisComponent component, AfterInteractUsingEvent args)
    {
        if (component.BrainEntity == null &&
            HasComp<BrainComponent>(args.Used) &&
            TryComp<BodyComponent>(uid, out var bodyComponent) &&
            bodyComponent.Root?.Child is { } root &&
            TryComp<BodyPartComponent>(root, out var bodyPartComponent) &&
            component.BrainWhitelist?.IsValid(args.Used) != false)
        {
            var slot = bodyPartComponent.Organs.Keys.Contains(component.BrainOrganSlotId)
                ? bodyPartComponent.Organs[component.BrainOrganSlotId]
                : _bobby.CreateOrganSlot(component.BrainOrganSlotId, root);
            if (slot == null)
                return;
            _bobby.InsertOrgan(args.Used, slot);
            component.BrainEntity = args.Used;
        }
    }
}
