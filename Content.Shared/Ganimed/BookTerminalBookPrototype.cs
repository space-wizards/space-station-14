using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Ganimed
{
    [Serializable, NetSerializable, Prototype("bookPrototype")]
    public sealed class BookTerminalBookPrototype : IPrototype
    {
        
		[DataField("name")]
        public string Name { get; set; } = "";
		
		[DataField("description")]
        public string Description { get; set; } = "";
		
		[DataField("content")]
        public string Content { get; set; } = "";
		
		[DataField("stampedBy")]
        public List<List<string>> StampedBy { get; set; } = new();
		
		[DataField("stampState")]
        public string StampState { get; set; } = "";

        [ViewVariables, IdDataField]
        public string ID { get; } = default!;
    }
}