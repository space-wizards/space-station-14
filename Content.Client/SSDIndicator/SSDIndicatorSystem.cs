using Content.Shared.CCVar;
using Content.Shared.SSDIndicator;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.SSDIndicator;

/// <summary>
///     Handles displaying SSD indicator as status icon
/// </summary>
public sealed class SSDIndicatorSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SSDIndicatorComponent, GetStatusIconsEvent>(OnGetStatusIcon);
    }

    private void OnGetStatusIcon(EntityUid uid, SSDIndicatorComponent component, ref GetStatusIconsEvent args)
    {
        if (!component.IsSSD ||
            !_cfg.GetCVar(CCVars.ICShowSSDIndicator) ||
            args.InContainer)
            return;

        args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(component.Icon));
    }
}
