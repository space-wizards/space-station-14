using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CosmicCult;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CosmicCultActionComponent : Component
{
    /// <summary>
    /// Whether this action is currently empowered by the holding cultist.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Empowered = false;

    [DataField]
    public EntProtoId Vfx = "EffectCosmicGeneric";

    [DataField]
    public SoundSpecifier Sfx = new SoundPathSpecifier("/Audio/Cosmic/Abilities/ability-ingress.ogg");
}

public sealed partial class EventCosmicSiphon : EntityTargetActionEvent;
public sealed partial class EventCosmicShunt : EntityTargetActionEvent;
public sealed partial class EventCosmicReturn : InstantActionEvent;
public sealed partial class EventCosmicLapse : EntityTargetActionEvent;
public sealed partial class EventCosmicGlare : InstantActionEvent;
public sealed partial class EventCosmicIngress : EntityTargetActionEvent;
public sealed partial class EventCosmicImposition : InstantActionEvent;
public sealed partial class EventCosmicNova : WorldTargetActionEvent;
public sealed partial class EventCosmicFragmentation : EntityTargetActionEvent;
public sealed partial class EventCosmicShift : InstantActionEvent;

// COLOSSUS ACTIONS
public sealed partial class EventCosmicColossusSunder : WorldTargetActionEvent;
public sealed partial class EventCosmicColossusIngress : EntityTargetActionEvent;
public sealed partial class EventCosmicColossusHibernate : InstantActionEvent;
