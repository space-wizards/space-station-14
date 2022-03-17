using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SubFloor;

[RegisterComponent]
[NetworkedComponent]
public sealed class TrayScannerComponent : Component
{
    /// <summary>
    ///     Whether the scanner is currently on.
    /// </summary>
    [ViewVariables]
    public bool Enabled { get; set; }

    /// <summary>
    ///     Last position of the scanner. Rounded to integers to avoid excessive entity lookups when moving.
    /// </summary>
    [ViewVariables]
    public Vector2i? LastLocation { get; set; }

    /// <summary>
    ///     Radius in which the scanner will reveal entities. Centered on the <see cref="LastLocation"/>.
    /// </summary>
    [DataField("range")]
    public float Range { get; set; } = 2.5f;

    /// <summary>
    ///     The sub-floor entities that this scanner is currently revealing.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> RevealedSubfloors = new();
}

[Serializable, NetSerializable]
public sealed class TrayScannerState : ComponentState
{
    public bool Enabled;

    public TrayScannerState(bool enabled)
    {
        Enabled = enabled;
    }
}
