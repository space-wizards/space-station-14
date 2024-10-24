using Content.Shared.Item.ItemToggle;
using Content.Shared.Weapons.Misc;
using Robust.Shared.Physics.Components;

namespace Content.Server.Weapons.Misc;

public sealed class COTelekinesisHandSystem : COSharedTelekinesisHandSystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override bool CanTether(EntityUid uid, COTelekinesisHandComponent component, EntityUid target, EntityUid? user)
    {
        if (!base.CanTether(uid, component, target, user))
            return false;

        return true;
    }

    protected override void StartTether(EntityUid gunUid, COTelekinesisHandComponent component, EntityUid target, EntityUid? user,
        PhysicsComponent? targetPhysics = null, TransformComponent? targetXform = null)
    {
        base.StartTether(gunUid, component, target, user, targetPhysics, targetXform);
        _toggle.TryActivate(gunUid);
    }

    public override void StopTether(EntityUid gunUid, COTelekinesisHandComponent component, bool land = true, bool transfer = false)
    {
        base.StopTether(gunUid, component, land, transfer);
        _toggle.TryDeactivate(gunUid);
    }
}
