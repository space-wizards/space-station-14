using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Content.Shared.Explosion.Components;

namespace Content.Server.Explosion.Components
{
    /// <summary>
    /// Specifies the energy and brisance (shattering effect) of an explosive component.
    /// Energy corresponds to how far the explosive shockwave will travel.
    /// Brisance governs the amount of damage done when the shockwave contacts an object (though damage also falls off with distance).
    /// </summary>
    [RegisterComponent]
    public class ExplosiveComponent : SharedExplosiveComponent
    {

    }
}
