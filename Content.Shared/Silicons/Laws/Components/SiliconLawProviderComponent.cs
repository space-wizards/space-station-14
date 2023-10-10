using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// This is used for an entity which grants laws to a <see cref="SiliconLawBoundComponent"/>
/// </summary>
[RegisterComponent, Access(typeof(SharedSiliconLawSystem))]
public sealed partial class SiliconLawProviderComponent : Component
{
    /// <summary>
    /// The laws that are provided.
    /// </summary>
	[DataField("lawset", customTypeSerializer: typeof(PrototypeIdSerializer<SiliconLawsetPrototype>))]
    public string Lawset = "Crewsimov";
	
	[DataField("lawsets", customTypeSerializer: typeof(PrototypeIdListSerializer<SiliconLawsetPrototype>))]
    public List<string> Lawsets = new();
	
    [DataField("laws", customTypeSerializer: typeof(PrototypeIdListSerializer<SiliconLawPrototype>))]
    public List<string> Laws = new();
	
	[DataField("name")]
    public string Name = "lawset-name-none";
	
	[DataField("description")]
    public string Description = "lawset-description-none";
	
}
