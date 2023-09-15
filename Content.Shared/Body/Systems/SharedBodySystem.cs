using Content.Shared.Damage;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Systems;

public abstract partial class SharedBodySystem : EntitySystem
{
    protected const string BodySlotContainerId = "BodyPartSlot";
    protected const string BodyRootContainerId = "BodyRootSlot";
    protected const string OrganSlotContainerId = "OrganSlot";

    [Dependency] protected readonly IPrototypeManager Prototypes = default!;

    [Dependency] protected readonly SharedContainerSystem Containers = default!;
    [Dependency] protected readonly DamageableSystem Damageable = default!;
    [Dependency] protected readonly StandingStateSystem Standing = default!;
    [Dependency] protected readonly MovementSpeedModifierSystem Movement = default!;
    [Dependency] protected readonly SharedTransformSystem SharedTransform = default!;
    public override void Initialize()
    {
        base.Initialize();

        InitializeBody();
        InitializeParts();
        InitializeOrgans();
    }

    public static string GetBodySlotContainerName(string slotName)
    {
        return BodyPartSlotPrefix + slotName;
    }

    public static string GetOrganContainerName(string slotName)
    {
        return OrganSlotContainerId + slotName;
    }

    public static string GetBodyRootContainerName(string slotName)
    {
        return BodyRootContainerId + slotName;
    }
}
