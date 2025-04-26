using Robust.Shared.Utility;

namespace Content.Shared._Harmony.EntitySelector;

[ImplicitDataDefinitionForInheritors]
public abstract partial class EntitySelector
{
    [Dependency] protected readonly IEntityManager EntityManager = default!;

    public bool Initialized { get; private set; }

    [DataField]
    public List<EntitySelector> SubSelectors = new();

    /// <summary>
    /// One-time initialization of an entity selector.
    /// Recursively initializes all sub-selectors.
    /// </summary>
    [MustCallBase]
    internal virtual void Initialize()
    {
        DebugTools.Assert(!Initialized, "Tried to initialize an entity selector twice.");

        IoCManager.InjectDependencies(this);

        Initialized = true;

        foreach (var subSelector in SubSelectors)
        {
            if (!subSelector.Initialized)
                subSelector.Initialize();
        }
    }

    /// <summary>
    /// Checks if the entity should get selected by the entity selector.
    /// </summary>
    [MustCallBase]
    public virtual bool Matches(EntityUid entity)
    {
        if (!Initialized)
            Initialize();

        foreach (var subSelector in SubSelectors)
        {
            if (!subSelector.Matches(entity))
                return false;
        }

        return true;
    }
}
