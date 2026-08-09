using Content.Shared.CCVar;
using Content.Shared.NewPlayer;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Configuration;

namespace Content.Client.NewPlayer;

/// <summary>
/// Used to visualize <see cref="NewPlayerIconComponent"/> status icons, if enabled on the server.
/// </summary>
public sealed partial class NewPlayerSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _configManager = default!;

    private bool _showPlayerIcons;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_configManager, CCVars.ShowNewPlayerIcons, v => _showPlayerIcons = v, true);
    }

    [SubscribeLocalEvent]
    private void GetNewPlayerIcon(Entity<NewPlayerIconComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_showPlayerIcons && ProtoMan.Resolve(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
