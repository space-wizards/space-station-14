// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.DeadSpace.TheCircle.Geist;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TheCircleGeistComponent : Component
{
    [DataField]
    public EntProtoId InvisibilityAction = "ActionTheCircleGeistInvisibility";

    [DataField, AutoNetworkedField]
    public EntityUid? InvisibilityActionEntity;

    [DataField]
    public List<GeistInvisibilityModeInfo> Modes = new()
    {
        new()
        {
            Mode = GeistInvisibilityMode.Escape,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/_DeadSpace/Demons/ability_icons/shadowling_blink_action.png")),
            Tooltip = "geist-invisibility-mode-escape",
        },
        new()
        {
            Mode = GeistInvisibilityMode.Phase,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/_DeadSpace/Demons/ability_icons/shadowling_ghost_action.png")),
            Tooltip = "geist-invisibility-mode-phase",
        },
        new()
        {
            Mode = GeistInvisibilityMode.Stationary,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/_DeadSpace/Demons/ability_icons/shadowling_veil_action.png")),
            Tooltip = "geist-invisibility-mode-stationary",
        },
    };

    [DataField]
    public TimeSpan EscapeDuration = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan EscapeCooldown = TimeSpan.FromSeconds(40);

    [DataField]
    public float EscapeSpeedModifier = 1.2f;

    [DataField]
    public float EscapeRunningVisibility = -1f;

    [DataField]
    public TimeSpan PhaseDuration = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan PhaseCooldown = TimeSpan.FromSeconds(40);

    [DataField]
    public float PhaseSpeedModifier = 0.75f;

    [DataField]
    public TimeSpan PhaseStunDuration = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan StationaryMovementGrace = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan StationaryCooldown = TimeSpan.FromSeconds(70);

    [DataField]
    public float StationaryFadeRate = 1.5f;

    [DataField]
    public float StationaryMovementVisibilityRate = 1.5f;

    [DataField]
    public float FullVisibility = -1f;

    [DataField]
    public float MovingVisibility = 0.5f;

    [DataField]
    public float MovementThreshold = 0.05f;

    [ViewVariables]
    public GeistInvisibilityMode? ActiveMode;

    [ViewVariables]
    public TimeSpan? EffectEnds;

    [ViewVariables]
    public TimeSpan? MovementStarted;

    [ViewVariables]
    public bool WasCollidable;

    [ViewVariables]
    public Dictionary<string, int> OriginalCollisionMasks = new();

    [ViewVariables]
    public bool HadStealthOnMove;

    [ViewVariables]
    public bool AmbushPacifismApplied;

    [ViewVariables]
    public float OriginalPassiveVisibilityRate;

    [ViewVariables]
    public float OriginalMovementVisibilityRate;

    [ViewVariables]
    public EntityCoordinates? PhaseLastSafeCoordinates;

    [ViewVariables]
    public TimeSpan? ModeSelectionExpires;

    [AutoNetworkedField, ViewVariables]
    public TimeSpan EscapeReadyAt;

    [AutoNetworkedField, ViewVariables]
    public TimeSpan PhaseReadyAt;

    [AutoNetworkedField, ViewVariables]
    public TimeSpan StationaryReadyAt;
}

[DataDefinition]
public sealed partial class GeistInvisibilityModeInfo
{
    [DataField(required: true)]
    public GeistInvisibilityMode Mode;

    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    [DataField(required: true)]
    public LocId Tooltip;
}

[Serializable, NetSerializable]
public enum GeistInvisibilityMode : byte
{
    Escape,
    Phase,
    Stationary,
}

public sealed partial class OpenGeistInvisibilityMenuEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed class SelectGeistInvisibilityModeEvent(NetEntity geist, GeistInvisibilityMode mode) : EntityEventArgs
{
    public NetEntity Geist = geist;
    public GeistInvisibilityMode Mode = mode;
}
