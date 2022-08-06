using System.Linq;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Projectiles.Components;
using Content.Server.Singularity.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Baseball
{
    /// <summary>
    /// This handles...
    /// </summary>
    public sealed class BallLauncherSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly StorageSystem _storageSystem = default!;

        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
            //SubscribeLocalEvent<BallLauncherComponent, PowerConsumerReceivedChanged>(ReceivedChanged);
            SubscribeLocalEvent<BallLauncherComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<BallLauncherComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<BallLauncherComponent, GetVerbsEvent<Verb>>(OnOtherVerbs);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var ballLauncher in EntityQuery<BallLauncherComponent>())
            {
                if (!ballLauncher.IsOn)
                    return;

                ballLauncher.AccumulatedFrametime += frameTime;

                if (ballLauncher.AccumulatedFrametime < ballLauncher.CurrentLauncherCooldown)
                    continue;

                ballLauncher.AccumulatedFrametime -= ballLauncher.CurrentLauncherCooldown;
                ballLauncher.CurrentLauncherCooldown = ballLauncher.ShootSpeed;


                Fire(ballLauncher.Owner);
            }
        }

        public void OnInteractHand(EntityUid uid, BallLauncherComponent component, InteractHandEvent args)
        {
            args.Handled = true;

            if (EntityManager.TryGetComponent(component.Owner, out PhysicsComponent? phys) && phys.BodyType == BodyType.Static)
            {
                if (!component.IsOn)
                {
                    component.IsOn = true;
                    _popupSystem.PopupEntity("Turned on", component.Owner, Filter.Pvs(component.Owner));
                }
                else
                {
                    component.IsOn = false;
                    _popupSystem.PopupEntity("Turned off", component.Owner, Filter.Pvs(component.Owner));

                }
            }

        }

        public void OnInteractUsing(EntityUid uid, BallLauncherComponent component, InteractUsingEvent args)
        {
            args.Handled = true;

            if (EntityManager.TryGetComponent<ItemComponent?>(args.Used, out var item) && EntityManager.TryGetComponent<ServerStorageComponent?>(component.Owner, out var storage))
            {
                if (_storageSystem.CanInsert(component.Owner, args.Used, out _, storage))
                {
                    _storageSystem.Insert(component.Owner, args.Used, storage); }
            }

        }

        private void Fire(EntityUid uid)
        {
            if (!TryComp<BallLauncherComponent>(uid, out var component))
                return;

            if (!EntityManager.TryGetComponent<ServerStorageComponent?>(component.Owner, out var storage))
                return;

            if (storage.StoredEntities == null)
                return;
            if (storage.StoredEntities.Count == 0)
                return;

            _popupSystem.PopupEntity("fire", component.Owner, Filter.Pvs(component.Owner));

            var projectile = _robustRandom.Pick(storage.StoredEntities);
            _storageSystem.RemoveAndDrop(uid, projectile, storage);

            /*
            if (!EntityManager.TryGetComponent<PhysicsComponent?>(projectile, out var physicsComponent))
                return;

            physicsComponent.BodyStatus = BodyStatus.InAir;
            */
            var dir = EntityManager.GetComponent<TransformComponent>(component.Owner).WorldRotation.ToWorldVec() * _robustRandom.NextFloat(20f, 50f);

            _audioSystem.Play(_audioSystem.GetSound(component.FireSound), Filter.Pvs(component.Owner), component.Owner, AudioParams.Default);

            _throwingSystem.TryThrow(projectile, dir, 10f, uid);

        }

        private void OnOtherVerbs(EntityUid uid, BallLauncherComponent component, GetVerbsEvent<Verb> args)
        {
            if (!args.CanInteract)
                return;

            Verb ejectItems = new();
            ejectItems.Act = () => TryEjectAllItems(component, args.User);
            ejectItems.Text = Loc.GetString("pneumatic-cannon-component-verb-eject-items-name");
            args.Verbs.Add(ejectItems);
        }

        public void TryEjectAllItems(BallLauncherComponent component, EntityUid user)
        {
            if (!EntityManager.TryGetComponent<ServerStorageComponent?>(component.Owner, out var storage))
                return;
            if (storage.StoredEntities == null)
                return;

            foreach (var entity in storage.StoredEntities.ToArray())
            {
                _storageSystem.RemoveAndDrop(component.Owner, entity, storage);
            }
            _popupSystem.PopupEntity(Loc.GetString("pneumatic-cannon-component-ejected-all", ("cannon", (component.Owner))), user, Filter.Local());
        }
    }
}
