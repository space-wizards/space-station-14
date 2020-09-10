using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Medical;
using Content.Server.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Content.Shared.GameObjects.Components.Medical;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Content.Server.GameObjects.EntitySystems
{
    internal sealed class CloningSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<CloningPodComponent>())
            {
                comp.Update(frameTime);
            }
        }

        public static Dictionary<int, Mind> Minds = new Dictionary<int, Mind>();

        public static void AddToDnaScans(Mind mind)
        {
            if (!Minds.ContainsValue(mind))
            {
                Minds.Add(Minds.Count(), mind);
            }
        }

        public static bool HasDnaScan(Mind mind)
        {
            return Minds.ContainsValue(mind);
        }

        public static Dictionary<int, string> getIdToUser()
        {
            return Minds.ToDictionary(m => m.Key, m => m.Value.CharacterName);
        }
    }
}
