namespace Content.Shared._Starlight.Railroading;

[RegisterComponent]
public sealed partial class RailroadCardPerformerComponent : Component
{
    [ViewVariables]
    [NonSerialized]
    public Entity<RailroadableComponent>? Performer;
}