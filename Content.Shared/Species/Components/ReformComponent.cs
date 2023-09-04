using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.GameStates;

namespace Content.Shared.Species.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ReformComponent : Component
{
    /// <summary>
    /// The action to use.
    /// </summary>
    [DataField("actionPrototype", required: true)]
    public string ActionPrototype;

    /// <summary>
    /// How long it will take to reform
    /// </summary>
    [DataField("reformTime", required: true)]
    public float ReformTime = 0;

    /// <summary>
    /// Whether or not the entity should start with a cooldown
    /// </summary>
    [DataField("startDelayed")]
    public bool StartDelayed = true;

    /// <summary>
    /// Whether or not the entity should be stunned when reforming at all
    /// </summary>
    [DataField("shouldStun")]
    public bool ShouldStun = true;

    /// <summary>
    /// The text that appears when attempting to reform
    /// </summary>
    [DataField("popupText", required: true)]
    public string PopupText;

    /// <summary>
    /// The mob that our entity will reform into
    /// </summary>
    [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
    public string? Prototype { get; private set; }
}
