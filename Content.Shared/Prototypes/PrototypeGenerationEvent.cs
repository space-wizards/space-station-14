namespace Content.Shared.Prototypes;

[ByRefEvent]
public readonly record struct PrototypeGenerationEvent(List<(string Id, EntityBuilder Builder)> Ents)
{
    public void AddEntity(string id, EntityBuilder builder)
    {
        Ents.Add((id, builder));
    }
}

public record struct EntityBuilder(
    string[]? Parents,
    string? Name,
    string? Description,
    string? Suffix,
    List<Component>? Components
)
{
    public EntityBuilder AddComp<T>(T comp) where T : Component
    {
        Components ??= new List<Component>();
        Components.Add(comp);
        return this;
    }
}
