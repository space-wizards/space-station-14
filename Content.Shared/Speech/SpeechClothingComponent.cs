using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Speech;

/// </summary>
/// This allows clothing to change the sound of the player's speech.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SpeechClothingSystem))]
public sealed class SpeechClothingComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<SpeechSoundsPrototype>))]
    public string Prototype = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("prev", required: false)]
    public string? Previous = default!;
}
