using Robust.Shared.Prototypes;

namespace Content.Shared.Cloning;

public abstract partial class SharedCloningSystem : EntitySystem
{
    /// <summary>
    /// Copy components from one entity to another based on a CloningSettingsPrototype.
    /// </summary>
    /// <param name="original">The orignal Entity to clone components from.</param>
    /// <param name="clone">The target Entity to clone components to.</param>
    /// <param name="settings">The clone settings prototype containing the list of components to clone.</param>
    public virtual void CloneComponents(EntityUid original, EntityUid clone, CloningSettingsPrototype settings)
    {
    }

    /// <summary>
    /// Copy components from one entity to another based on a CloningSettingsPrototype.
    /// </summary>
    /// <param name="original">The orignal Entity to clone components from.</param>
    /// <param name="clone">The target Entity to clone components to.</param>
    /// <param name="settings">The clone settings prototype id containing the list of components to clone.</param>
    public virtual void CloneComponents(EntityUid original, EntityUid clone, ProtoId<CloningSettingsPrototype> settings)
    {
    }
}
