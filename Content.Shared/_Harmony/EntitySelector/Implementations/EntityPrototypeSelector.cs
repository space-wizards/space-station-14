using Robust.Shared.Prototypes;

namespace Content.Shared._Harmony.EntitySelector.Implementations;

public sealed partial class EntityPrototypeSelector : EntitySelector
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    /// <inheritdoc />
    public override bool Matches(EntityUid entity)
    {
        if (!base.Matches(entity))
            return false;

        if (!EntityManager.TryGetComponent<MetaDataComponent>(entity, out var metaData))
            return false;

        if (metaData.EntityPrototype != null &&
            metaData.EntityPrototype.ID == Prototype)
            return true;

        return false;
    }
}
