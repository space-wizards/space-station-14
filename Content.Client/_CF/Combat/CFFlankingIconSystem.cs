using Content.Shared.CF.Combat;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.CF.Combat;

/// <summary>
/// Draws the flanked mark on the right side of an off-guard creature. Unlike job icons, it does not require HUD glasses.
/// </summary>
public sealed partial class CFFlankingIconSystem : EntitySystem
{
    private static readonly ProtoId<StatusIconPrototype> Icon = "CFFlankedIcon";

    [SubscribeLocalEvent]
    private void OnGetStatusIcons(Entity<CFOffGuardComponent> ent, ref GetStatusIconsEvent args)
    {
        if (!ent.Comp.Active)
            return;

        args.StatusIcons.Add(ProtoMan.Index(Icon));
    }
}
