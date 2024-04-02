using Robust.Shared.Prototypes;

namespace Content.Shared.CrystallPunk.LockKey;

/// <summary>
/// A prototype of the lock category. Need a roundstart mapping to ensure that keys and locks will fit together despite randomization.
/// </summary>
[Prototype("CPLockCategory")]
public sealed partial class CPLockCategoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The number of elements that will be generated for the category.
    /// </summary>
    [DataField] public int Complexity = 3;
}
