using Robust.Shared.Prototypes;

namespace Content.Shared.Radio
{
    [Prototype("radioChannel")]
    public sealed class RadioChannelPrototype : IPrototype
    {
        [ViewVariables] [DataField("name")] public string Name { get; private set; } = string.Empty;
        [ViewVariables] [DataField("keycode")] public char KeyCode { get; private set; } = '\0';
        [ViewVariables] [DataField("channel")] public int Channel { get; private set; } = 0;

        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;
    }
}
