using Robust.Shared.Prototypes;

namespace Content.Shared.CrystallPunk.LockKey;

/// <summary>
/// a key component that can be used to unlock and lock locks from CPLockComponent
/// </summary>
[RegisterComponent]
public sealed partial class CPKeyComponent : Component
{
    [DataField]
    public List<int>? LockShape = null;

    /// <summary>
    /// If not null, automatically generates a key for the specified category on initialization. This ensures that the lock will be opened with a key of the same category.
    /// </summary>
    [DataField]
    public ProtoId<CPLockCategoryPrototype>? AutoGenerateShape = null;
}
