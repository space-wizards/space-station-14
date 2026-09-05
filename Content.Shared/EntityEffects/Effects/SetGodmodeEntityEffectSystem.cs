using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Enables or disables godmode on this entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class SetGodmodeEntityEffectSystem : EntityEffectSystem<MetaDataComponent, SetGodmode>
{
    [Dependency] private SharedGodmodeSystem _godmode = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<SetGodmode> args)
    {
        if (args.Effect.Enabled == HasComp<GodmodeComponent>(entity))
            return;

        if (args.Effect.Enabled)
            _godmode.EnableGodmode(entity);
        else
            _godmode.DisableGodmode(entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SetGodmode : EntityEffectBase<SetGodmode>
{
    [DataField(required: true)]
    public bool Enabled;
}
