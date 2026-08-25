namespace Content.Shared.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicExamineComponent : Component
{
    [DataField(required: true)]
    public LocId CultistText;

    [DataField]
    public LocId OthersText = "cosmic-examine-text-structures";
}
