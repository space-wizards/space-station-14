using Content.Shared.EntityConditions;

namespace Content.Shared.Objectives.Systems;

/// <summary>
/// The building block for your own target system, doesn't even have its return values defined!
/// TODO: If engine ever makes Entity<T> share an interface with EntityUid, make this a GenericTargetSystem<T> where T : IWhateverTheFuck
/// </summary>
public abstract partial class GenericTargetSystem : EntitySystem
{
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    protected bool ValidEntity(EntityUid uid, EntityUid? exclude = null, params EntityCondition[] conditions)
    {
        return exclude != uid && _conditions.TryConditions(uid, conditions, exclude);
    }
}
