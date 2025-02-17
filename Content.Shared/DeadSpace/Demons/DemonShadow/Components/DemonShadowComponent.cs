// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared.NPC.Prototypes;

namespace Content.Shared.DeadSpace.Demons.DemonShadow.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class DemonShadowComponent : Component
{
    [DataField]
    public EntProtoId DemonShadowGrapple = "ActionDemonShadowGrapple";

    [DataField, AutoNetworkedField]
    public EntityUid? DemonShadowGrappleActionEntity;

    [DataField]
    public EntProtoId DemonShadowCrawl = "ActionDemonShadowCrawl";

    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<NpcFactionPrototype>? OldFaction;

    [DataField, AutoNetworkedField]
    public EntityUid? DemonShadowCrawlActionEntity;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsShadowCrawl = true;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsStartShadowCrawl = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsShadowPosition = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? TeleportTarget = null;

    [DataField]
    public TimeSpan CheckDuration = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan TimeToCheck = TimeSpan.Zero;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TeleportDuration = TimeSpan.FromSeconds(1);

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilTeleport = TimeSpan.Zero;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan ShadowCrawlDuration = TimeSpan.FromSeconds(1.2);

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilShadowCrawl = TimeSpan.Zero;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextTickForRegen = TimeSpan.FromSeconds(0);

    [ViewVariables(VVAccess.ReadOnly)]
    public float MovementSpeedMultiply = 1f;

    [ViewVariables(VVAccess.ReadOnly)]
    public float PassiveHealingMultiplier = 4f;

    [DataField("passiveHealing")]
    public DamageSpecifier PassiveHealing = new()
    {
        DamageDict = new()
        {
            { "Blunt", -0.4 },
            { "Slash", -0.2 },
            { "Piercing", -0.2 },
            { "Heat", -0.4 },
            { "Shock", -0.1 },
            { "Bloodloss", -0.2 }
        }
    };

    #region Visualizer
    [DataField("state")]
    public string State = "running";

    [DataField("hideState")]
    public string HideState = "hide";

    [DataField("astralState")]
    public string AstralState = "astral";
    #endregion
}
