namespace Content.Shared.Atmos.Components;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class IgniteOnMeleeHitComponent : Component
{
    [DataField("fireStacks"), AutoNetworkedField]
    public float FireStacks;
}
