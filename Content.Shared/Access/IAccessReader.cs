using Robust.Shared.Prototypes;

namespace Content.Shared.Access;

public interface IAccessReader
{
    HashSet<ProtoId<AccessLevelPrototype>> GetAccesses();
}
