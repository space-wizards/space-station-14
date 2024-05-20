using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Server.Tiles;

/// <summary>
/// Applies flammable and damage while vaulting.
/// </summary>
[RegisterComponent, Access(typeof(TileEntityEffectSystem))]
public sealed partial class TileEntityEffectComponent : Component
{
    /// <summary>
    /// How many fire stacks are applied per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public List<EntityEffect> Effects = new();
}
