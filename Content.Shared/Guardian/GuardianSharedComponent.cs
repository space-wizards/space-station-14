using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Guardian
{
    [RegisterComponent]
    public class GuardianSharedComponent : Component
    {
        public override string Name => "GuardianShared";

        public EntityUid Host;

        public EntityUid Guardian;

        public float HealthPercent;

        public float AllowedDistance;

        public float CurrentDistance;

        public bool Guardianloose = false;
    }
}
