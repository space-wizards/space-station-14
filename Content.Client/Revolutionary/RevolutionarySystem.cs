using Content.Shared.Revolutionary.Components;
using Content.Client.Antag;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Ghost;
using Robust.Client.Player;

namespace Content.Client.Revolutionary;

/// <summary>
/// Used for the client to get status icons from other revs.
/// </summary>
public sealed class RevolutionarySystem : AntagStatusIconSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RevolutionaryComponent, GetStatusIconsEvent>(GetRevIcon);
    }

    /// <summary>
    /// Checks if the person who triggers the GetStatusIcon event is also a Rev or a HeadRev.
    /// </summary>
    private void GetRevIcon(EntityUid uid, RevolutionaryComponent comp, ref GetStatusIconsEvent args)
    {
        if (!HasComp<RevolutionaryComponent>(_player.LocalPlayer?.ControlledEntity))
        {
            if (HasComp<GhostComponent>(_player.LocalPlayer?.ControlledEntity))
            {

            }
            else return;
        }

        if (TryComp<HeadRevolutionaryComponent>(uid, out var head))
        {
            GetStatusIcon(comp.RevStatusIcon, comp.HeadRevStatusIcon, ref args);
        }
        else
        {
            GetStatusIcon(comp.RevStatusIcon, null, ref args);
        }
    }
}
