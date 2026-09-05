using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Adds an action unless this entity already has one from the same prototype.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class AddActionEntityEffectSystem : EntityEffectSystem<MetaDataComponent, AddAction>
{
    [Dependency] private SharedActionsSystem _actions = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<AddAction> args)
    {
        var actionPrototype = args.Effect.Action;
        foreach (var action in _actions.GetActions(entity))
        {
            if (actionPrototype.Equals(MetaData(action).EntityPrototype?.ID))
                return;
        }

        _actions.AddAction(entity, actionPrototype);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AddAction : EntityEffectBase<AddAction>
{
    [DataField(required: true)]
    public EntProtoId Action;
}
