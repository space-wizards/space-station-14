using Robust.Shared.Prototypes;

namespace Content.Shared.CollectiveMind;

[Prototype("collectiveMind")]
public sealed partial class CollectiveMindPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; } = default!;

    [DataField("name")]
    public string Name { get; private set; } = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    [DataField("keycode")]
    public char KeyCode { get; private set; } = '\0';

    [DataField("color")]
    public Color Color { get; private set; } = Color.Lime;

    [DataField("requiredComponents")]
    public List<string> RequiredComponents { get; set; } = new();

    [DataField("requiredTags")]
    public List<string> RequiredTags { get; set; } = new();
}
