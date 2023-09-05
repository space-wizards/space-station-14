using Content.Shared.Damage;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Client.Overlays;

/// <summary>
/// Shows a healthy icon on mobs.
/// </summary>
public sealed class ShowHealthIconsSystem : EquipmentHudSystem<ShowHealthIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeMan = default!;

    public HashSet<string> DamageContainers = new();

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string HealthIconFine = "HealthIconFine";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageableComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);

    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ShowHealthIconsComponent> component)
    {
        base.UpdateInternal(component);

        foreach (var damageContainerId in component.Components.SelectMany(x => x.DamageContainers))
        {
            DamageContainers.Add(damageContainerId);
        }
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        DamageContainers.Clear();
    }

    private void OnGetStatusIconsEvent(EntityUid uid, DamageableComponent damageableComponent, ref GetStatusIconsEvent args)
    {
        if (!IsActive)
            return;

        var healthIcons = DecideHealthIcon(uid, damageableComponent);

        args.StatusIcons.AddRange(healthIcons);
    }

    private IReadOnlyList<StatusIconPrototype> DecideHealthIcon(EntityUid uid, DamageableComponent damageableComponent)
    {
        var result = new List<StatusIconPrototype>();
        if (damageableComponent.DamageContainerID == null ||
            !DamageContainers.Contains(damageableComponent.DamageContainerID))
        {
            return result;
        }

        if (EntityManager.TryGetComponent<MetaDataComponent>(uid, out var metaDataComponent) &&
            (metaDataComponent.Flags & MetaDataFlags.InContainer) == MetaDataFlags.InContainer)
        {
            return result;
        }

        // Here you could check health status, diseases, mind status, etc. and pick a good icon, or multiple depending on whatever.
        if (damageableComponent?.DamageContainerID == "Biological" &&
            _prototypeMan.TryIndex<StatusIconPrototype>(HealthIconFine, out var healthyIcon))
        {
            result.Add(healthyIcon);
        }

        return result;
    }
}
