using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
///     This component allows you to see criminal record status of mobs.
/// </summary>
[RegisterComponent, NetworkedComponent, GenericEvent]
public sealed partial class ShowCriminalRecordIconsComponent : Component { }
