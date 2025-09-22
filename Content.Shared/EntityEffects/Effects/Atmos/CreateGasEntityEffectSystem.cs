using Content.Shared.Atmos;

namespace Content.Shared.EntityEffects.Effects.Atmos;

public abstract partial class SharedCreateGasEntityEffectSystem : EntityEffectSystem<TransformComponent, CreateGas>
{
    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<CreateGas> args)
    {
        // Server side effect
    }
}

[DataDefinition]
public sealed partial class CreateGas : EntityEffectBase<CreateGas>
{
    /// <summary>
    ///     The gas we're creating
    /// </summary>
    [DataField]
    public Gas Gas;

    /// <summary>
    ///     Mol modifier for gas creation.
    /// </summary>
    [DataField]
    public float Multiplier = 3f;
}
