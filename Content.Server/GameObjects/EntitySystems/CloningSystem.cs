using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Medical;
using Content.Server.Mobs;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    internal sealed class CloningSystem : EntitySystem, IResettingEntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<CloningPodComponent>(true))
            {
                comp.Update(frameTime);
            }
        }

        public readonly Dictionary<int, Mind> Minds = new();

        public void AddToDnaScans(Mind mind)
        {
            if (!Minds.ContainsValue(mind))
            {
                Minds.Add(Minds.Count, mind);
            }
        }

        public bool HasDnaScan(Mind mind)
        {
            return Minds.ContainsValue(mind);
        }

        public Dictionary<int, string> GetIdToUser()
        {
            return Minds.ToDictionary(m => m.Key, m => m.Value.CharacterName);
        }

        public void Reset()
        {
            Minds.Clear();
        }
    }
}
