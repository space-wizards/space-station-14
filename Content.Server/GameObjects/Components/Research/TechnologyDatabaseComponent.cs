using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.Components.Research
{
    [RegisterComponent]
    public class TechnologyDatabaseComponent : SharedTechnologyDatabaseComponent
    {
        public override ComponentState GetComponentState()
        {
            return new TechnologyDatabaseState(_technologies);
        }

        public bool UnlockTechnology(TechnologyPrototype technology)
        {
            if (_technologies.Contains(technology)) return false;
            foreach (var requiredTech in technology.RequiredTechnologies)
            {
                if (!IoCManager.Resolve<PrototypeManager>().TryIndex(requiredTech, out technology)) return false;
                if (!_technologies.Contains(technology)) return false;
            }
            _technologies.Add(technology);
            Dirty();
            return true;
        }

        public bool UnlockTechnology(string id)
        {
            return UnlockTechnology((TechnologyPrototype)IoCManager.Resolve<PrototypeManager>().Index(typeof(TechnologyPrototype), id));
        }

        public bool IsTechnologyUnlocked(TechnologyPrototype technology)
        {
            return _technologies.Contains(technology);
        }
    }
}
