using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Sprite;
using Robust.Client.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client.ADT
{
    public sealed class TailAnimantionSystem : EntitySystem
    {
        public FixedPoint2 LastDamage;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamageChanged);

        }

        private void OnDamageChanged(EntityUid uid, DamageableComponent component, DamageChangedEvent args)
        {
            if (component == null)
            {
                return;
            }

            if (component.TotalDamage > 100f && LastDamage < 100f)
            {
                if (!TryComp<SpriteComponent>(uid, out var sprite))
                { return; }

                foreach (var item in sprite.AllLayers)
                {
                    if (item.RsiState.Name != null)
                        if (item.RsiState.Name.ToLower().Contains("tail"))
                        {
                            item.AutoAnimated = false;
                            item.AnimationTime = 0;
                        }
                }
            }

            if (component.TotalDamage < 100f && LastDamage > 100f)
            {
                if (!TryComp<SpriteComponent>(uid, out var sprite))
                { return; }

                foreach (var item in sprite.AllLayers)
                {
                    if (item.RsiState.Name != null)
                        if (item.RsiState.Name.ToLower().Contains("tail"))
                        {
                            item.AnimationTime = 0;
                            item.AutoAnimated = true;
                        }
                }
            }
            LastDamage = component.TotalDamage;
        }
    }
}
