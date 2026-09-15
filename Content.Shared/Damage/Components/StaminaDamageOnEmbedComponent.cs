using Content.Shared.Damage.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Applies stamina damage when embeds in an entity.
/// </summary>
[NetworkedComponent]
[Access(typeof(SharedStaminaSystem))]
public sealed partial class StaminaDamageOnEmbedComponent : StaminaDamageComponent;
