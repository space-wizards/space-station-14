using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Abstract.Extensions;
public static class EntityExtensions
{
    public static bool TryGetEntProtoId(this EntityPrototype? entityPrototype, out EntProtoId protoId)
    {
        if (entityPrototype == null)
        {
            protoId = default;
            return false;
        }
        protoId = new EntProtoId(entityPrototype.ID);
        return true;
    }
}
