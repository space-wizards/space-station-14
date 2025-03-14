using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.CosmicCult;

[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicCultActionComponent : Component { }
public sealed partial class EventCosmicSiphon : EntityTargetActionEvent { }
public sealed partial class EventCosmicBlank : EntityTargetActionEvent { }
public sealed partial class EventCosmicPlaceMonument : InstantActionEvent { } //given to the cult leader on roundstart
public sealed partial class EventCosmicMoveMonument : InstantActionEvent { } //given the the cult leader on hitting tier 2, taken away on hitting tier 3
public sealed partial class EventCosmicReturn : InstantActionEvent { }
public sealed partial class EventCosmicLapse : EntityTargetActionEvent { }
public sealed partial class EventCosmicGlare : InstantActionEvent { }
public sealed partial class EventCosmicIngress : EntityTargetActionEvent { }
public sealed partial class EventCosmicImposition : InstantActionEvent { }
public sealed partial class EventCosmicNova : WorldTargetActionEvent { }


// Rogue Ascended
public sealed partial class EventRogueCosmicNova : WorldTargetActionEvent { }
public sealed partial class EventRogueInfection : EntityTargetActionEvent { }
public sealed partial class EventRogueGrandShunt : InstantActionEvent { }
public sealed partial class EventRogueShatter : EntityTargetActionEvent { }
