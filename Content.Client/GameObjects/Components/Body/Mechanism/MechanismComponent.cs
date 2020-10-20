#nullable enable
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Body.Mechanism
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMechanismComponent))]
    [ComponentReference(typeof(IMechanism))]
    public class MechanismComponent : SharedMechanismComponent { }
}
