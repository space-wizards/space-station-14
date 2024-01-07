using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.Paint;

[RegisterComponent, NetworkedComponent]
public sealed partial class PaintRemoverComponent : Component
{
    /// <summary>
    /// Sound when target is cleaned.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/watersplash.ogg");

    /// <summary>
    /// DoAfter wait time.
    /// </summary>
    [DataField]
    public float CleanDelay = 2f;
}

