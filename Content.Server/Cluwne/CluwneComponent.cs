using Content.Server.Speech.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Cluwne;

[RegisterComponent]
public sealed class CluwneComponent : Component
{
    /// <summary>
    /// Giggle emote chances.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float GiggleChance = 0.2f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float GiggleCooldown = 5;

    [ViewVariables(VVAccess.ReadWrite)]
    public float RandomGiggleAttempt = 5;

    [ViewVariables(VVAccess.ReadWrite)]
    public float LastGiggleCooldown = 0f;

    /// <summary>
    ///gigle emote timespan.
    /// </summary>
    [DataField("giggleGoChance", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan GiggleGoChance = TimeSpan.Zero;

    /// <summary>
    /// amount of time cluwne is paralyzed for when falling over.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float ParalyzeTime = 2f;

    /// <summary>
    /// sound specifiers for giggles and noises.
    /// </summary>
    [DataField("giggle")]
    public SoundSpecifier Giggle = new SoundCollectionSpecifier("CluwneScreams");

    [DataField("spawnsound")]
    public SoundSpecifier SpawnSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

    [DataField("knocksound")]
    public SoundSpecifier KnockSound = new SoundPathSpecifier("/Audio/Items/airhorn.ogg");
}
