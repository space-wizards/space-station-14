using Content.Server.Administration.Managers;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.CCVar;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Emag.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Roles;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Silicons.Borgs;

/// <inheritdoc/>
public sealed partial class BorgSystem : SharedBorgSystem
{
    [Dependency] private readonly IBanManager _banManager = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly PlayTimeTrackingSystem _playTime = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    private static readonly List<ProtoId<JobPrototype>> BorgJobId = ["Borg"];
    private bool _borgingRequirementCvar;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _configuration.OnValueChanged(CCVars.BorgingRequirementsCheck, (b) => { _borgingRequirementCvar = b; }, true);

        InitializeTransponder();
    }

    public override bool CanPlayerBeBorged(ICommonSession session, out string reason)
    {
        reason = string.Empty;

        if (_banManager.IsRoleBanned(session, BorgJobId))
        {
            reason = Loc.GetString("role-ban");
            return false;
        }

        //TODO Put the "required role" on each chassis prototype, instead of having a standard hardcoded one
        // Then we won't need the cvar either.
        if (_borgingRequirementCvar && !_playTime.IsAllowed(session, BorgJobId, out var message))
        {
            reason = message?.ToString() ?? "";
            return false;
        }

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateTransponder(frameTime);
    }
}
