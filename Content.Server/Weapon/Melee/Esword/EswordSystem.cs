using System.Linq;
using Robust.Shared.GameObjects;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Log;

namespace Content.Server.Weapon.Melee.Esword
{
    internal class EswordSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EswordComponent, MeleeHitEvent>(OnMeleeHit);

        }

        private void OnMeleeHit(EntityUid uid, EswordComponent comp, MeleeHitEvent args)
        {
            if (!comp.Activated || !args.HitEntities.Any())
                return;
            
            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.HitSound.GetSound(), comp.Owner, AudioHelpers.WithVariation(0.25f));
        }
    }
}
