using Robust.Shared.Serialization;

namespace Content.Shared.Sticky.Components;

﻿using DrawDepth;

/// <summary>
/// Sets the sprite's draw depth depending on whether it's stuck.
/// </summary>
[RegisterComponent]
public sealed partial class StickyVisualizerComponent : Component
{
    /// <summary>
    /// What sprite draw depth gets set to when stuck to something.
    /// </summary>
    [DataField]
    public int StuckDrawDepth = (int) DrawDepth.Overdoors;

    /// <summary>
    /// The sprite's original draw depth before being stuck.
    /// </summary>
    [DataField]
    public int OriginalDrawDepth;
}

[Serializable, NetSerializable]
public enum StickyVisuals : byte
{
    IsStuck
}
