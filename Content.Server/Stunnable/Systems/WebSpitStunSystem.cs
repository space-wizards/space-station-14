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



                _stunSystem.TryParalyze(target, TimeSpan.FromSeconds(component.ParalyzeTime), true);


                CuffableComponent cuffed;
                EntityManager.TryGetComponent(target, out cuffed!);
                var cuffs = EntityManager.SpawnEntity("BroodyTrap", EntityManager.GetComponent<TransformComponent>(target).Coordinates);
                _cuff.TryAddNewCuffs(target, target, cuffs, cuffed);

                _popup.PopupEntity(Loc.GetString("Ваши руки связанны паутиной!"), target, target, PopupType.LargeCaution);



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

    }

}
