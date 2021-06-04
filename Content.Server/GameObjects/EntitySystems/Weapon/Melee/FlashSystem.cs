using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Weapon;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.GameObjects.EntitySystems.Weapon.Melee
{
    public class FlashSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FlashComponent, ClickAttackEvent>(OnClickAttack);
            SubscribeLocalEvent<FlashComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<FlashComponent, UseInHandEvent>(OnUseInHand);

            SubscribeLocalEvent<FlashComponent, ExaminedEvent>(OnExamined);
        }

        public void OnClickAttack(EntityUid uid, FlashComponent comp, ClickAttackEvent args)
        {
            if (!UseFlash(comp, args.User))
            {
                return;
            }

            if (args.TargetEntity != null)
                FlashEntity(args.TargetEntity, args.User, comp.FlashDuration, comp.SlowTo);
        }

        public void OnAfterInteract(EntityUid uid, FlashComponent comp, AfterInteractEvent args)
        {
            if (!UseFlash(comp, args.User))
            {
                return;
            }

            if (args.Target != null)
                FlashEntity(args.Target, args.User, comp.FlashDuration, comp.SlowTo);
        }

        public void OnUseInHand(EntityUid uid, FlashComponent comp, UseInHandEvent args)
        {
            if (!UseFlash(comp, args.User))
            {
                return;
            }

            foreach (var entity in IoCManager.Resolve<IEntityLookup>().GetEntitiesInRange(comp.Owner.Transform.Coordinates, comp.Range))
            {
                FlashEntity(entity, args.User, comp.AoeFlashDuration, comp.SlowTo);
            }
        }

        private bool UseFlash(FlashComponent comp, IEntity user)
        {
            if (comp.HasUses)
            {
                // TODO flash visualizer
                if (!comp.Owner.TryGetComponent<SpriteComponent>(out var sprite))
                    return false;

                if (--comp.Uses == 0)
                {
                    sprite.LayerSetState(0, "burnt");
                    comp.Owner.PopupMessage(user, Loc.GetString("flash-component-becomes-empty"));
                }
                else if (!comp.Flashing)
                {
                    int animLayer = sprite.AddLayerWithState("flashing");
                    comp.Flashing = true;

                    comp.Owner.SpawnTimer(400, () =>
                    {
                        sprite.RemoveLayer(animLayer);
                        comp.Flashing = false;
                    });
                }

                SoundSystem.Play(Filter.Pvs(comp.Owner), "/Audio/Weapons/flash.ogg", comp.Owner.Transform.Coordinates,
                    AudioParams.Default);

                return true;
            }

            return false;
        }

        // TODO: Check if target can be flashed (e.g. things like sunglasses would block a flash)
        // TODO: Merge with the code in FlashableComponent--raise an event on the target, that FlashableComponent or
        // another comp will catch
        private void FlashEntity(IEntity target, IEntity user, float flashDuration, float slowTo)
        {
            if (target.TryGetComponent<FlashableComponent>(out var flashable))
            {
                flashable.Flash(flashDuration / 1000d);
            }

            if (target.TryGetComponent<StunnableComponent>(out var stunnableComponent))
            {
                stunnableComponent.Slowdown(flashDuration / 1000f, slowTo, slowTo);
            }

            if (target != user)
            {
                user.PopupMessage(target,
                    Loc.GetString(
                        "flash-component-user-blinds-you",
                        ("user", user)
                    )
                );
            }
        }

        private void OnExamined(EntityUid uid, FlashComponent comp, ExaminedEvent args)
        {
            if (!comp.HasUses)
            {
                args.Message.AddText("\n");
                args.Message.AddText(Loc.GetString("flash-component-examine-empty"));
                return;
            }

            if (args.IsInDetailsRange)
            {
                args.Message.AddText("\n");
                args.Message.AddMarkup(
                    Loc.GetString(
                        "flash-component-examine-detail-count",
                        ("count", comp.Uses),
                        ("markupCountColor", "green")
                    )
                );
            }
        }
    }
}
