using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Silicons.Laws;

[DataDefinition, Serializable, NetSerializable]
public partial class SiliconLawset
{
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public List<SiliconLaw> Laws = new();
	
	[DataField]
    public string? Name;
	
	[DataField]
    public string? Description;
	
	public string LoggingString()
    {
        var laws = new List<string>(Laws.Count);
        foreach (var law in Laws)
        {
            laws.Add($"{law.Order}: {Loc.GetString(law.LawString)}");
        }

        return string.Join(" / ", laws);
    }
	
	public SiliconLawset Clone()
    {
        var laws = new List<SiliconLaw>(Laws.Count);
        foreach (var law in Laws)
        {
            laws.Add(law.ShallowClone());
        }

        return new SiliconLawset()
        {
            Laws = laws,
			Name = Name,
			Description = Description
			
        };
    }
}

/// <summary>
/// This is a prototype for a <see cref="SiliconLawPrototype"/> list.
/// Cannot be used directly since it is a list of prototype ids rather than List<Siliconlaw>.
/// </summary>
[Prototype("siliconLawset"), Serializable, NetSerializable]
public sealed partial class SiliconLawsetPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// List of law prototype ids in this lawset.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<SiliconLawPrototype>))]
    public List<string> Laws = new();
	
	[DataField("name")]
    public string? Name;
	
	[DataField("description")]
    public string? Description;


}
