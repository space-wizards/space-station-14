using Content.Server.Speech.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Cluwne;

[RegisterComponent]
public sealed class CluwneComponent : Component
{
    /// The chance that on a random attempt
    /// the cluwne will do an emote
    [ViewVariables(VVAccess.ReadWrite)]
    public float GiggleChance = 0.2f;

    /// Minimum time between giggles
    [ViewVariables(VVAccess.ReadWrite)]
    public float GiggleCooldown = 5;

    /// The length of time between each cluwne random giggle
    /// attempt.
    [ViewVariables(VVAccess.ReadWrite)]
    public float RandomGiggleAttempt = 5;

    [ViewVariables(VVAccess.ReadWrite)]
    public float LastDamageGiggleCooldown = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Accumulator = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float ParalyzeTime = 2;
	
	[DataField("giggle")]
    public SoundSpecifier Giggle = new SoundCollectionSpecifier("CluwneScreams");

    //honks when called.
    [DataField("spawnsound")]
    public SoundSpecifier SpawnSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

    //makes an airhorn noise when called.
    [DataField("knocksound")]
    public SoundSpecifier KnockSound = new SoundPathSpecifier("/Audio/Items/airhorn.ogg");
}
