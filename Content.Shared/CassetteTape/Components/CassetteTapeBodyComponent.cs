using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.CassetteTape.Components;

/// <summary>
///     This component enables cassette tape related interactions (e.g., entity white-lists, cell sizes, examine, rigging).
///     The actual cassette functionality is provided by the server-side CassetteTapeComponent.
/// </summary>
[NetworkedComponent]
[RegisterComponent]
public sealed partial class CassetteTapeBodyComponent : Component
{
}

