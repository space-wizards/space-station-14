using Content.Shared.Damage.Components;
using Content.Shared.Rejuvenate;
using Robust.Shared.GameStates;

namespace Content.Shared.StatusEffectNew.Components;

/// <summary>
/// Marker component for a status effect that should be removed on
/// rejuvenation and should not be applied on targets with
/// <see cref="GodmodeComponent"/>.
/// </summary>
/// <seealso cref="RejuvenateEvent"/>
/// <remarks> Pen name is RejuvenateableStatusEffectComponent </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class CurableStatusEffectComponent : Component;
