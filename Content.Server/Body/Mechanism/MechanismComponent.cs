#nullable enable
using Content.Shared.Body.Mechanism;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.Mechanism
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMechanismComponent))]
    public class MechanismComponent : SharedMechanismComponent
    {
    }
}
