namespace Content.Server.Singularity.Components
{
    [RegisterComponent]
    public sealed class SingularityGeneratorComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

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
