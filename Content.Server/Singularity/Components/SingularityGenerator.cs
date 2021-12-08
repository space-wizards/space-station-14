using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.Singularity.Components
{
    [RegisterComponent]
    public class SingularityGeneratorComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override string Name => "SingularityGenerator";

        [ViewVariables] private int _power;

        public int Power
        {
            get => _power;
            set
            {
                if(_power == value) return;

                _power = value;
                if (_power > 15)
                {
                    _entMan.SpawnEntity("Singularity", _entMan.GetComponent<TransformComponent>(Owner).Coordinates);
                    //dont delete ourselves, just wait to get eaten
                }
            }
        }
    }
}
