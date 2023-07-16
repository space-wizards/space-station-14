using Content.Shared.StatusEffects.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.StatusEffects.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedStatusEffectsSystem))]
public sealed class StatusEffectsComponent : Component
{
    [DataField("statusContainerId")]
    public string StatusContainerId = "status-effect-container";

    [DataField("effectsWhitelist", customTypeSerializer: typeof(PrototypeIdSerializer<StatusEffectWhitelistPrototype>))]
    public string? EffectsWhitelist;

    [ViewVariables(VVAccess.ReadWrite)]
    public Container? StatusContainer = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextActivation = TimeSpan.Zero;
}

