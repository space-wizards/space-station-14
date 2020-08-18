using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    internal sealed class CloningSystem : EntitySystem
    {
        public static List<EntityUid> scannedUids = new List<EntityUid>();

        public static void AddToScannedUids(EntityUid uid)
        {
            if (!scannedUids.Contains(uid))
            {
                scannedUids.Add(uid);
            }
        }

        public static bool HasUid(EntityUid uid)
        {
            return scannedUids.Contains(uid);
        }
    }
}
