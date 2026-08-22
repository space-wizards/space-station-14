using Content.Server.Administration.Managers;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Roles;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Silicons.Borgs;

/// <inheritdoc/>
public sealed partial class BorgSystem : SharedBorgSystem
{
    [Dependency] private IBanManager _banManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private TriggerSystem _trigger = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private GhostRoleSystem _ghostRole = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    public static readonly ProtoId<JobPrototype> BorgJobId = "Borg";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        InitializeTransponder();
    }

    protected override void OnMMILinkedRemoved(Entity<MMIComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        base.OnMMILinkedRemoved(ent, ref args);

        if (_mind.TryGetMind(ent, out var mindId, out var mind))
        {
            if (HasComp<GhostTakeoverAvailableComponent>(ent))
                // We detach the ghost role player from the brain if they leave the MMI, as they are not the original brain's owner.
                _mind.TransferTo(mindId, null, true, true, mind: mind);
            else
                _mind.TransferTo(mindId, ent.Owner, true, mind: mind);
        }

        if (HasComp<GhostTakeoverAvailableComponent>(ent))
        {
            RemCompDeferred<GhostTakeoverAvailableComponent>(ent);
            RemCompDeferred<GhostRoleComponent>(ent);
        }
    }

    public override bool CanPlayerBeBorged(ICommonSession session)
    {
        if (_banManager.GetJobBans(session.UserId)?.Contains(BorgJobId) == true)
            return false;

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateTransponder(frameTime);
    }

    protected override void EnableGhostRole(Entity<MMIComponent> entity)
    {
        if (!entity.Comp.EnableGhostRole || entity.Comp.GhostRole == null)
            return;

        var ghostRole = EnsureComp<GhostRoleComponent>(entity.Owner);
        EnsureComp<GhostTakeoverAvailableComponent>(entity.Owner);

        //GhostRoleComponent inherits custom settings from the the MMI component
        _ghostRole.ApplyGhostRoleSettings((entity.Owner, ghostRole), entity.Comp.GhostRole);
    }
}
