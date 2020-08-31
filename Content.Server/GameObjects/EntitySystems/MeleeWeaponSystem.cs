using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems
{
    public sealed class MeleeWeaponSystem : EntitySystem
    {
        public void SendAnimation(string arc, Angle angle, IEntity attacker, IEnumerable<IEntity> hits)
        {
            RaiseNetworkEvent(new MeleeWeaponSystemMessages.PlayMeleeWeaponArcAnimationMessage(arc, angle, attacker.Uid,
                hits.Select(e => e.Uid).ToList()));
        }

        public void SendAnimation(Angle angle, IEntity attacker, IEntity hit)
        {
            RaiseNetworkEvent(new MeleeWeaponSystemMessages.PlayMeleeWeaponAnimationMessage(angle, attacker.Uid, hit.Uid));
        }
    }
}
