using Content.Shared.Revolutionary.Components;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Revolutionary;

/// <summary>
/// Used for the client to get status icons from other revs.
/// </summary>
public sealed class RevolutionarySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RevolutionaryComponent, GetStatusIconsEvent>(OnGetStatusIcon);
    }

    /// <summary>
    /// Checks if you have the revolutionary or head rev component and gets status icons from other revs. 
    /// </summary>
    private void OnGetStatusIcon(EntityUid uid, RevolutionaryComponent component, ref GetStatusIconsEvent args)
    {
        if (!HasComp<RevolutionaryComponent>(_player.LocalPlayer?.ControlledEntity) && !HasComp<HeadRevolutionaryComponent>(_player.LocalPlayer?.ControlledEntity))
            return;

        if (HasComp<RevolutionaryComponent>(uid) && !HasComp<HeadRevolutionaryComponent>(uid))
        {
            args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(component.RevStatusIcon));
        }
        else
        {
            args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(component.HeadRevStatusIcon));
        }
    }
}
