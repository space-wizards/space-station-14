using Content.Shared.Revolutionary.Components;
using Content.Shared.Revolutionary;
using Content.Shared.StatusIcon.Components;

namespace Content.Client.Revolutionary;

/// <summary>
/// Used for the client to get status icons from other revs.
/// </summary>
public sealed partial class RevolutionarySystem : SharedRevolutionarySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevolutionaryComponent, GetStatusIconsEvent>(GetRevIcon);
        SubscribeLocalEvent<HeadRevolutionaryComponent, GetStatusIconsEvent>(GetHeadRevIcon);
    }

    private void GetRevIcon(Entity<RevolutionaryComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<HeadRevolutionaryComponent>(ent))
            return;

        if (ProtoMan.Resolve(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GetHeadRevIcon(Entity<HeadRevolutionaryComponent> ent, ref GetStatusIconsEvent args)
    {
        if (ProtoMan.Resolve(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
