using System.Linq;
using Content.Server.Popups;
using Content.Server.Sports.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Sports
{
    public sealed class PitchingMachineSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly StorageSystem _storageSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            //SubscribeLocalEvent<PitchingMachineComponent, PowerConsumerReceivedChanged>(ReceivedChanged);
            //SubscribeLocalEvent<PitchingMachineComponent, InteractHandEvent>(OnInteractHand);
            //SubscribeLocalEvent<PitchingMachineComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<PitchingMachineComponent, GetVerbsEvent<Verb>>(AddEjectVerb);
            SubscribeLocalEvent<PitchingMachineComponent, GetVerbsEvent<AlternativeVerb>>(AddPowerVerb);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var ballLauncher in EntityQuery<PitchingMachineComponent>())
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
        public void TogglePower(EntityUid uid, PitchingMachineComponent component)
        {
            if (EntityManager.TryGetComponent(component.Owner, out PhysicsComponent? phys) && phys.BodyType == BodyType.Static)
            {
                if (!component.IsOn)
                {
                    component.IsOn = true;
                    _popupSystem.PopupEntity(Loc.GetString("comp-emitter-turned-on", ("target", component.Owner)), component.Owner, Filter.Pvs(component.Owner));
                }
                else
                {
                    component.IsOn = false;
                    _popupSystem.PopupEntity(Loc.GetString("comp-emitter-turned-off", ("target", component.Owner)), component.Owner, Filter.Pvs(component.Owner));
                }
            }
        }
/*
        public void OnInteractUsing(EntityUid uid, PitchingMachineComponent component, InteractUsingEvent args)
        {
            args.Handled = true;

            if (EntityManager.TryGetComponent<ToolComponent?>(args.Used, out var tool))
            {
                if (tool.Qualities.Contains("Anchoring"))
                    return;
            }

            if (EntityManager.TryGetComponent<ItemComponent?>(args.Used, out var item) && EntityManager.TryGetComponent<ServerStorageComponent?>(component.Owner, out var storage))
            {
                if (_storageSystem.CanInsert(component.Owner, args.Used, out _, storage))
                {
                    _storageSystem.Insert(component.Owner, args.Used, storage);
                }
            }

        }
*/
        private void Fire(EntityUid uid)
        {
            if (!TryComp<PitchingMachineComponent>(uid, out var component))
                return;

            if (!EntityManager.TryGetComponent<ServerStorageComponent?>(component.Owner, out var storage))
                return;

            if (storage.StoredEntities == null)
                return;

            if (storage.StoredEntities.Count == 0)
                return;

            var projectile = _robustRandom.Pick(storage.StoredEntities);
            _storageSystem.RemoveAndDrop(uid, projectile, storage);

            /*
            if (!EntityManager.TryGetComponent<PhysicsComponent?>(projectile, out var physicsComponent))
                return;

            physicsComponent.BodyStatus = BodyStatus.InAir;
            */
            var dir = EntityManager.GetComponent<TransformComponent>(component.Owner).WorldRotation.ToWorldVec() * _robustRandom.NextFloat(component.ShootDistanceMin, component.ShootDistanceMax);

            _audioSystem.Play(_audioSystem.GetSound(component.FireSound), Filter.Pvs(component.Owner), component.Owner, AudioParams.Default);

            _throwingSystem.TryThrow(projectile, dir, 10f, uid);

        }

        private void AddPowerVerb(EntityUid uid, PitchingMachineComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            // You don't get to toggle power if it's unanchored
            if (EntityManager.TryGetComponent<TransformComponent>(component.Owner, out var transformComponent) && !transformComponent.Anchored)
                return;
            AlternativeVerb togglePower = new();
            togglePower.Act = () => TogglePower(uid, component);
            togglePower.Text = Loc.GetString("Toggle Power");
            args.Verbs.Add(togglePower);
        }

        private void AddEjectVerb(EntityUid uid, PitchingMachineComponent component, GetVerbsEvent<Verb> args)
        {
            if (!args.CanInteract)
                return;

            Verb ejectItems = new();
            ejectItems.Act = () => TryEjectAllItems(component, args.User);
            ejectItems.Text = Loc.GetString("pneumatic-cannon-component-verb-eject-items-name");
            args.Verbs.Add(ejectItems);
        }

        public void TryEjectAllItems(PitchingMachineComponent component, EntityUid user)
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
