using Content.Shared.Revolutionary.Components;
using Content.Shared.Revolutionary;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Revolutionary;

/// <summary>
/// Used for the client to get status icons from other revs.
/// </summary>
public sealed class RevolutionarySystem : SharedRevolutionarySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

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

        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GetHeadRevIcon(Entity<HeadRevolutionaryComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
