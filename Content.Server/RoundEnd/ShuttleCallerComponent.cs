namespace Content.Server.RoundEnd;

/// <summary>
///     Given to any item (e.g. communications boards) capable of calling a round end shuttle.
/// </summary>
/// <remarks>
///     Used by <see cref="ShuttleCallerFailsafeSystem" /> to determine if there is any way to call the shuttle remaining on the station. 
/// </remarks>
[RegisterComponent]
public sealed partial class ShuttleCallerComponent : Component;
