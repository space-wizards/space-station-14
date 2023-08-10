using Content.Server.Stunnable.Components;
using JetBrains.Annotations;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Spider;
using Content.Server.Popups;
using Robust.Server.GameObjects;
using Content.Shared.Actions;
using Content.Server.Weapons.Ranged.Systems;
using Content.Server.Actions;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Popups;
using Content.Server.Polymorph.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;

namespace Content.Server.Stunnable
{
    [UsedImplicitly]
    internal sealed class WebSpitStunSystem : EntitySystem
    {
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly PhysicsSystem _physics = default!;
        [Dependency] private readonly GunSystem _gunSystem = default!;
        [Dependency] private readonly ActionsSystem _action = default!;
        [Dependency] private readonly SharedCuffableSystem _cuff = default!;
        [Dependency] private readonly PolymorphSystem _poly = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WebSpitStunComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<WebSpitStunComponent, SpiderWebSpitActionEvent>(OnWebSpit);

            SubscribeLocalEvent<WebStunComponent, StartCollideEvent>(HandleCollide);
            SubscribeLocalEvent<WebStunComponent, ThrowDoHitEvent>(HandleThrow);
        }


        public sealed class SpiderWebSpitActionEvent : WorldTargetActionEvent
        {

        }

        private void OnWebSpit(EntityUid uid, WebSpitStunComponent component, SpiderWebSpitActionEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            var webBullet = Spawn(component.BulletWebSpitId, Transform(uid).Coordinates);
            var xform = Transform(uid);
            var mapCoords = args.Target.ToMap(EntityManager);
            var direction = mapCoords.Position - xform.MapPosition.Position;
            var userVelocity = _physics.GetMapLinearVelocity(uid);

            _gunSystem.ShootProjectile(webBullet, direction, userVelocity, uid, uid);
            //_audioSystem.PlayPvs(component.WebSpitStunComponent, uid, component.WebSpitStunComponent.Params);
        }


        private void TryDoCollideStun(EntityUid uid, WebStunComponent component, EntityUid target)
        {
            {
                if (HasComp<SpiderComponent>(target))
                    return;
                if (!HasComp<HumanoidAppearanceComponent>(target))
                    return;
                if (TryComp(target, out MobStateComponent? mobState))
                {
                    if (mobState.CurrentState is not MobState.Alive)
                    {
                        return;
                    }
                }




                if (!HasComp<TransformComponent>(target))
                {
                    EntityManager.AddComponent<TransformComponent>(target);
                }


                var polytarget = _poly.PolymorphEntity(target, component.WebPolymorph);

                if (polytarget == null)
                {
                    return;
                }

                EntityManager.AddComponent<CoconComponent>(polytarget.Value);

                if (TryComp(polytarget, out CoconComponent? CoconComp))
                    CoconComp.EquipedOn = polytarget.Value;


                _stunSystem.TryParalyze(polytarget.Value, TimeSpan.FromSeconds(component.ParalyzeTime), true);

                if (TryComp(polytarget, out CuffableComponent? cuffcomp))
                {
                    var cuffs = Spawn(component.WebTrap, Transform(polytarget.Value).Coordinates);
                    _cuff.TryAddNewCuffs(polytarget.Value, polytarget.Value, cuffs, cuffcomp);
                    _popup.PopupEntity(Loc.GetString("Your hands are tied with cobwebs!"), polytarget.Value, polytarget.Value, PopupType.LargeCaution);
                }
                var broodCocon = Spawn("BroodCocon", Transform(polytarget.Value).Coordinates);
                _inventory.TryEquip(polytarget.Value, broodCocon, "outerClothing", true, true);
            }

        }


        private void HandleCollide(EntityUid uid, WebStunComponent component, ref StartCollideEvent args)
        {
            if (args.OurFixture.ID != component.FixtureID) return;

            TryDoCollideStun(uid, component, args.OtherEntity);
        }

        private void HandleThrow(EntityUid uid, WebStunComponent component, ThrowDoHitEvent args)
        {
            TryDoCollideStun(uid, component, args.Target);
        }

        private void OnStartup(EntityUid uid, WebSpitStunComponent component, ComponentStartup args)
        {
            _action.AddAction(uid, component.ActionWebSpit, null);
        }


        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var comp in EntityQuery<CoconComponent>())
            {
                comp.Accumulator += frameTime;

                if (comp.Accumulator <= comp.DamageFrequency)
                    continue;

                comp.Accumulator = 0;

                if (comp.EquipedOn is not { Valid: true } targetId)
                    continue;
                if (TryComp(targetId, out MobStateComponent? mobState))
                {
                    if (mobState.CurrentState is not MobState.Alive)
                    {
                        _poly.Revert(targetId);
                    }
                }

                if (TryComp(targetId, out HandsComponent? handComp))
                {
                    foreach (var hand in _hands.EnumerateHands(targetId, handComp))
                    {
                        if (hand.HeldEntity != null)
                        {
                            return;
                        }
                        else
                            _poly.Revert(targetId);
                    }

                }
            }
        }


    }

}
