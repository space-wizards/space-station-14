namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed partial class PowerProviderComponent : Component
{
    [DataField]
    public HashSet<EntityUid> LinkedReceivers = new();
}
