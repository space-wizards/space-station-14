using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Singularity
{

    [RegisterComponent]
    [ComponentReference(typeof(IClientSingularityInstance))]
    public class ToySingularityComponent : Component, IClientSingularityInstance
    {
        public override string Name => "ToySingularity";
        public int Level {
            get {
                return 1;
            }
            set {
            }
        }
    }
}
