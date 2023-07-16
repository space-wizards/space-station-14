using Content.Shared.StatusEffects;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.StatusEffects.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedStatusEffectsSystem))]
public sealed class StatusEffectsComponent : Component
{
    [DataField("statusContainerId")]
    public string StatusContainerId = "status-effect-container";

    [ViewVariables(VVAccess.ReadWrite)]
    public Container? StatusContainer = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextActivation = TimeSpan.Zero;
}

