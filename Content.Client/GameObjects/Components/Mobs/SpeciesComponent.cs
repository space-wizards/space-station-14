using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSpeciesComponent))]
    public class SpeciesComponent : SharedSpeciesComponent
    {

    }
}
