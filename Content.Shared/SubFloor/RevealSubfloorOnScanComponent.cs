using Robust.Shared.GameStates;

namespace Content.Shared.SubFloor;

/// <summary>
/// Added in tile-like entities that should reveal subfloor entities when scanning for t-rays.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class RevealSubfloorOnScanComponent : Component;
