using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class SingularityGeneratorComponent : Component
    {
        public override string Name => "SingularityGenerator";

        private int _power;

        public int Power
        {
            get => _power;
            set
            {
                if(_power == value) return;

                _power = value;
                if (_power > 15)
                {
                    var entityManager = IoCManager.Resolve<IEntityManager>();
                    entityManager.SpawnEntity("Singularity", Owner.Transform.Coordinates);
                    //dont delete ourselves, just wait to get eaten
                }
            }
        }
    }
}
