

using Content.Shared.Chemistry.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry;

public sealed class SharedMedipenRefiller
{
    [Serializable, NetSerializable]
    public enum MedipenRefillerUiKey
    {
        Key
    }

}

[Serializable, NetSerializable]
public sealed class MedipenRefillerUpdateState : BoundUserInterfaceState
{
    public List<ProtoId<MedipenRecipePrototype>> Recipes;
    public MedipenRefillerUpdateState(List<ProtoId<MedipenRecipePrototype>> recipes)
    {
        Recipes = recipes;
    }
}

[Serializable, NetSerializable]
public sealed class MedipenRefillerSyncRequestMessage : BoundUserInterfaceMessage
{
}
