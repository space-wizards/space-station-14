using Content.Shared.Damage;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Client.Overlays;

/// <summary>
/// Shows a healthy icon on mobs.
/// </summary>
public sealed class ShowHealthIconsSystem : EquipmentHudSystem<ShowHealthIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeMan = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public HashSet<string> DamageContainers = new();

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string HealthIconHealthy = "HealthIconHealthy";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string HealthIconCrit = "HealthIconCrit";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string HealthIconDead = "HealthIconDead";

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
        if (!IsActive || args.InContainer)
            return;

        var healthIcons = DecideHealthIcons(damageableComponent, uid);

        args.StatusIcons.AddRange(healthIcons);
    }

    private IReadOnlyList<StatusIconPrototype> DecideHealthIcons(DamageableComponent damageableComponent, EntityUid uid)
    {
        if (damageableComponent.DamageContainerID == null ||
            !DamageContainers.Contains(damageableComponent.DamageContainerID))
        {
            return Array.Empty<StatusIconPrototype>();
        }

        var result = new List<StatusIconPrototype>();

        // Here you could check health status, diseases, mind status, etc. and pick a good icon, or multiple depending on whatever.
        // note - Maybe I did something wrong. But it work!
        if (damageableComponent?.DamageContainerID == "Biological" &&
            _prototypeMan.TryIndex<StatusIconPrototype>(HealthIconHealthy, out var healthyIcon) && TryComp<MobStateComponent>(uid, out var damageState))
        {
            if ( _mobState.IsCritical(uid, damageState) &&
                _prototypeMan.TryIndex<StatusIconPrototype>(HealthIconCrit, out var healthyIconCrit))
            {
                result.Add(healthyIconCrit);
            }
            else if (_mobState.IsDead(uid, damageState) &&
                     _prototypeMan.TryIndex<StatusIconPrototype>(HealthIconDead, out var healthyIconDead))
            {
                result.Add(healthyIconDead);
            }
            else
            {
                result.Add(healthyIcon);
            }
        }

        return result;
    }
}
