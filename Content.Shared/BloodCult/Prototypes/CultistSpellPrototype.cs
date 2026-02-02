using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.BloodCult.Prototypes;

[Prototype]
public sealed partial class CultAbilityPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    ///     What event should be raised
    /// </summary>
    [DataField] public object? Event;

    /// <summary>
    ///     What actions should be given
    /// </summary>
    [DataField] public List<EntProtoId>? ActionPrototypes;

	/// <summary>
	///		Health drain to prepare this spell.
	/// </summary>
	[DataField] public int HealthDrain = 7;

	/// <summary>
	///		Length of DoAfter to carve this spell.
	/// </summary>
	[DataField] public int DoAfterLength = 5;

	/// <summary>
    ///     Sound that plays when used to carve a spell.
    /// </summary>
    [DataField] public SoundSpecifier CarveSound = new SoundCollectionSpecifier("gib");
}
