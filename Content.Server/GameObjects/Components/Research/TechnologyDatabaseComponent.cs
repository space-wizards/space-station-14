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

        public bool SyncWithServer()
        {
            if (!Owner.TryGetComponent(out ResearchClientComponent client)) return false;
            if (!client.ConnectedToServer) return false;

            foreach (var tech in client.Server.UnlockedTechnologies)
            {
                if (!IsTechnologyUnlocked(tech)) UnlockTechnology(tech);
            }

            foreach (var tech in _technologies)
            {
                if (!client.Server.IsTechnologyUnlocked(tech)) client.Server.Database.UnlockTechnology(tech);
            }

            Dirty();
            client.Server.Database.Dirty();

            return true;
        }

        public bool UnlockTechnology(TechnologyPrototype technology)
        {
            if (_technologies.Contains(technology)) return false;
            var prototypeMan = IoCManager.Resolve<IPrototypeManager>();
            foreach (var requiredTech in technology.RequiredTechnologies)
            {
                if (!prototypeMan.TryIndex(requiredTech, out technology)) return false;
                if (!_technologies.Contains(technology)) return false;
            }
            _technologies.Add(technology);
            Dirty();
            return true;
        }

        public bool IsTechnologyUnlocked(TechnologyPrototype technology)
        {
            return _technologies.Contains(technology);
        }
    }
}
