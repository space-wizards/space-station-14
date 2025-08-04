namespace Content.Shared.Speech.EntitySystems;

/// <summary>
///     An abstract entity system inheritor for accent systems.
/// </summary>
public abstract class AccentSystem<T> : EntitySystem
    where T: Component
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<T, AccentGetEvent>(OnAccent);
    }

    protected virtual void OnAccent(Entity<T> entity, ref AccentGetEvent args)
    {
        args.Message = Accentuate(entity, args.Message);
    }

    // Override to apply accent rules.
    // Entity is optional; make sure to handle cases where the accent is applied without an entity supplied.
    public virtual string Accentuate(Entity<T>? entity, string message) { return message; }
}
