using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NullLink;

[Prototype("titleBuilder")]
public sealed partial class TitleBuilderPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public List<TitleSegment> Segments = [];

    [DataField]
    public string Separator = "-";
}

// In each segment, the first text in the list will be selected. 
[DataDefinition]
public sealed partial class TitleSegment
{
    [DataField]
    public List<Title> Titles = [];
}

[DataDefinition]
public sealed partial class Title
{
    [DataField(required: true)]
    public string Text = "";

    [DataField(required: true)]
    public ulong[] Roles = [];

    [DataField]
    public Color? Color;
}