using Content.Shared.Revolutionary.Components;
using Content.Client.Antag;
using Content.Shared.StatusIcon.Components;

namespace Content.Client.Revolutionary;

/// <summary>
/// Used for the client to get status icons from other revs.
/// </summary>
public sealed class RevolutionarySystem : AntagStatusIcons<RevolutionaryComponent, HeadRevolutionaryComponent>
{
    protected override void GetStatusIcon(EntityUid uid, RevolutionaryComponent antag, HeadRevolutionaryComponent? leader, string antagStatusIcon, string? antagLeaderStatusIcon, ref GetStatusIconsEvent args)
    {
        base.GetStatusIcon(uid, antag, leader, antagStatusIcon, antagLeaderStatusIcon, ref args);
    }
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
        GetStatusIcon(uid, component, null, component.RevStatusIcon, component.HeadRevStatusIcon, ref args);
    }
}
