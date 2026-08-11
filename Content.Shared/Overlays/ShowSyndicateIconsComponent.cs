using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
///     This component allows you to identify members of the Syndicate faction.
/// </summary>
[RegisterComponent, NetworkedComponent, GenericEvent]
public sealed partial class ShowSyndicateIconsComponent : Component {}
