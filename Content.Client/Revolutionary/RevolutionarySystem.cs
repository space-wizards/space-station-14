using Content.Shared.Revolutionary.Components;
using Content.Client.Antag;
using Content.Shared.StatusIcon.Components;

namespace Content.Client.Revolutionary;

/// <summary>
/// Used for the client to get status icons from other revs.
/// </summary>
public sealed class RevolutionarySystem : AntagStatusIconSystem<RevolutionaryComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevolutionaryComponent, GetStatusIconsEvent>(GetRevIcon);
        SubscribeLocalEvent<HeadRevolutionaryComponent, GetStatusIconsEvent>(GetHeadRevIcon);
    }

    /// <summary>
    /// Checks if the person who triggers the GetStatusIcon event is also a Rev or a HeadRev.
    /// </summary>
    private void GetRevIcon(EntityUid uid, RevolutionaryComponent comp, ref GetStatusIconsEvent args)
    {
        if (!HasComp<HeadRevolutionaryComponent>(uid))
        {
            GetStatusIcon(comp.RevStatusIcon, ref args);
        }
    }

    private void GetHeadRevIcon(EntityUid uid, HeadRevolutionaryComponent comp, ref GetStatusIconsEvent args)
    {
        GetStatusIcon(comp.HeadRevStatusIcon, ref args);
    }
}
