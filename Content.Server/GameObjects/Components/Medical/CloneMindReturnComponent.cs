using Content.Server.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Medical
{
    [RegisterComponent]
    public class CloneMindReturnComponent : Component
    {
        public override string Name => "CloneMindReturn";

        [ViewVariables]
        public Mind? Mind = default;

        [ViewVariables]
        public EntityUid Parent;
    }
}
