using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;

namespace Content.Shared.Cluwne;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class CluwneComponent : Component
{
    /// <summary>
    /// timings for giggles and knocks.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DamageGiggleCooldown = TimeSpan.FromSeconds(2);

    [ViewVariables(VVAccess.ReadWrite)]
    public float KnockChance = 0.05f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float GiggleRandomChance = 0.1f;

    [DataField("emoteId", customTypeSerializer: typeof(PrototypeIdSerializer<EmoteSoundsPrototype>))]
    public string? EmoteSoundsId = "Cluwne";

    /// <summary>
    /// Should be true if this is a cluwne.
    /// </summary>
    [DataField]
    public bool IsCluwne = true;

    /// <summary>
    /// The autoemote sound to play.
    /// </summary>
    [DataField]
    public string AutoEmoteSound = "CluwneGiggle";

    /// <summary>
    /// Portal proto id.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public EntProtoId Portal = "PortalGreeny";

    /// <summary>
    /// Amount of time cluwne is paralyzed for when falling over.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float ParalyzeTime = 2f;

    /// <summary>
    /// Sound specifiers for honk and knock.
    /// </summary>
    [DataField]
    public SoundSpecifier SpawnSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

    [DataField]
    public SoundSpecifier KnockSound = new SoundPathSpecifier("/Audio/Items/airhorn.ogg");

    [DataField]
    public SoundSpecifier CluwneSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/Magic/staff_animation.ogg");

    /// <summary>
    /// Portal sound for beast arrival.
    /// </summary>
    [DataField]
    public SoundSpecifier ArrivalSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    //#region Starlight

    /// <summary>
    /// whether this cluwne is permanent and should be unremovable by bible thwacks
    /// </summary>
    [DataField]
    public bool Unremovable = false;

    //#endregion Starlight

}
