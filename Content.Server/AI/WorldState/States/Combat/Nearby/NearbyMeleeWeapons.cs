using System.Collections.Generic;
using Content.Server.AI.Components;
using Content.Server.AI.Utils;
using Content.Server.Weapon.Melee.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.WorldState.States.Combat.Nearby
{
    [UsedImplicitly]
    public sealed class NearbyMeleeWeapons : CachedStateData<List<EntityUid>>
    {
        public override string Name => "NearbyMeleeWeapons";

        protected override List<EntityUid> GetTrueValue()
        {
            var result = new List<EntityUid>();
            var entMan = IoCManager.Resolve<IEntityManager>();

            if (!entMan.TryGetComponent(Owner, out AiControllerComponent? controller))
            {
                return result;
            }

            foreach (var entity in Visibility.GetNearestEntities(entMan.GetComponent<TransformComponent>(Owner).Coordinates, typeof(MeleeWeaponComponent), controller.VisionRadius))
            {
                result.Add(entity);
            }

            return result;
        }
    }
}
