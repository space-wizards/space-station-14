// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Antag;
using Content.Shared.Ghost;
using Content.Shared.SS220.AdmemeEvents;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.AdmemeEvents;

/// <summary>
/// Used for the client to get status icons from other event roles.
/// </summary>
public sealed class EventRoleIconsSystem : AntagStatusIconSystem<EventRoleComponent>
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EventRoleComponent, GetStatusIconsEvent>(GetRoleIcon);
    }

    /// <summary>
    /// Obtains a status icon from proto id mentioned in component
    /// </summary>
    private void GetRoleIcon(EntityUid uid, EventRoleComponent comp, ref GetStatusIconsEvent args)
    {
        GetStatusIcon(comp, ref args);
    }

    private void GetStatusIcon(EventRoleComponent targetRoleComp, ref GetStatusIconsEvent args)
    {
        var ent = _player.LocalPlayer?.ControlledEntity;

        if (!HasComp<GhostComponent>(ent))
        {
            if (!TryComp<EventRoleComponent>(ent, out var playerRoleComp))
                return;
            else if (targetRoleComp.RoleGroupKey != playerRoleComp.RoleGroupKey)
                return;
        }

        args.StatusIcons.Add(_prototype.Index(targetRoleComp.StatusIcon));
    }
}
