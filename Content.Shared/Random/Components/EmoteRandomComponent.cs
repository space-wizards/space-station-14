using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Random;

[RegisterComponent]
public sealed class EmoteRandomComponent : Component
{
    /// <summary>
    /// Giggle emote chances.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float EmoteRandomChance = 0.2f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float EmoteRandomAttempt = 5;

    [ViewVariables(VVAccess.ReadWrite)]
    public float LastEmoteRandomCooldown = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float LastDamageEmoteRandomCooldown = 0f;

    [DataField("giggleGoChance", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan EmoteOnRandomChance = TimeSpan.Zero;
}
