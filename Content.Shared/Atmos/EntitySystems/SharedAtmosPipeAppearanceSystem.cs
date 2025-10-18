using Content.Shared.Atmos.Components;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosPipeAppearanceSystem : EntitySystem
{
    /// <summary>
    /// Returns the max number of pipe layers supported by a entity.
    /// </summary>
    /// <param name="uid">The entity being checked.</param>
    /// <param name="atmosPipeLayers">The entity's <see cref="AtmosPipeLayersComponent"/>, if available.</param>
    /// <returns>Returns <see cref="AtmosPipeLayersComponent.NumberOfPipeLayers"/>
    /// if the entity has the component, or 1 if it does not.</returns>
    protected int GetNumberOfPipeLayers(EntityUid uid, out AtmosPipeLayersComponent? atmosPipeLayers)
    {
        return TryComp(uid, out atmosPipeLayers) ? atmosPipeLayers.NumberOfPipeLayers : 1;
    }
}
