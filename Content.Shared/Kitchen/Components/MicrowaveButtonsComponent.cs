namespace Content.Shared.Kitchen.Components;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class MicrowaveButtonsComponent : Component
{
    [DataField, AutoNetworkedField]
    public int NumberOfButtons = 6;
}
