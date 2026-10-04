using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Atmos;

namespace Content.Server.EntityEffects.Effects.Atmos;

/// <summary>
/// Purges the contents of a gas canister.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class DumpCanisterEntityEffectSystem : EntityEffectSystem<TransformComponent, DumpCanister>
{
    [Dependency] private GasCanisterSystem _gasCanister = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<DumpCanister> args)
    {
        _gasCanister.PurgeContents(entity);
    }
}
