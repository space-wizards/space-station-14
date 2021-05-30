using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Player;

namespace Content.Server.GameObjects.EntitySystems
{
    public sealed class MeleeWeaponSystem : EntitySystem
    {
        public void SendAnimation(string arc, Angle angle, IEntity attacker, IEntity source, IEnumerable<IEntity> hits, bool textureEffect = false, bool arcFollowAttacker = true)
        {
            RaiseNetworkEvent(new MeleeWeaponSystemMessages.PlayMeleeWeaponAnimationMessage(arc, angle, attacker.Uid, source.Uid,
                hits.Select(e => e.Uid).ToList(), textureEffect, arcFollowAttacker), Filter.Pvs(source, 1f));
        }

        public void SendLunge(Angle angle, IEntity source)
        {
            RaiseNetworkEvent(new MeleeWeaponSystemMessages.PlayLungeAnimationMessage(angle, source.Uid), Filter.Pvs(source, 1f));
        }
    }
}
