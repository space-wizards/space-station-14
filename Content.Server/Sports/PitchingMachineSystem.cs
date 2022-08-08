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
            SubscribeLocalEvent<PitchingMachineComponent, GetVerbsEvent<Verb>>(AddEjectVerb);
            SubscribeLocalEvent<PitchingMachineComponent, GetVerbsEvent<AlternativeVerb>>(AddPowerVerb);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var ballLauncher in EntityQuery<PitchingMachineComponent>())
            {
                ballLauncher.AccumulatedFrametime += frameTime;

                if (ballLauncher.AccumulatedFrametime < ballLauncher.CurrentLauncherCooldown)
                    continue;

                ballLauncher.AccumulatedFrametime -= ballLauncher.CurrentLauncherCooldown;
                ballLauncher.CurrentLauncherCooldown = ballLauncher.ShootCooldown;

                if (ballLauncher.IsOn)
                    Fire(ballLauncher.Owner);
            }
        }

        public void TogglePower(EntityUid uid, PitchingMachineComponent component)
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

        private void Fire(EntityUid uid)
        {
            if (!TryComp<PitchingMachineComponent>(uid, out var component))
                return;
            if (!TryComp<ServerStorageComponent?>(component.Owner, out var storage))
                return;
            if (storage.StoredEntities == null)
                return;
            if (storage.StoredEntities.Count == 0)
                return;

            var projectile = _robustRandom.Pick(storage.StoredEntities);
            _storageSystem.RemoveAndDrop(uid, projectile, storage);

            var dir = Comp<TransformComponent>(component.Owner).WorldRotation.ToWorldVec() * _robustRandom.NextFloat(component.ShootDistanceMin, component.ShootDistanceMax);

            _audioSystem.Play(_audioSystem.GetSound(component.FireSound), Filter.Pvs(component.Owner), component.Owner, AudioParams.Default);
            _throwingSystem.TryThrow(projectile, dir, 10f, uid);

        }

        private void AddPowerVerb(EntityUid uid, PitchingMachineComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || args.Hands == null)
                return;

            AlternativeVerb togglePower = new();
            togglePower.Act = () => TogglePower(uid, component);
            togglePower.Text = Loc.GetString("Toggle Power");
            args.Verbs.Add(togglePower);
        }

        private void AddEjectVerb(EntityUid uid, PitchingMachineComponent component, GetVerbsEvent<Verb> args)
        {
            if (!args.CanInteract || args.Hands == null)
                return;

            Verb ejectItems = new();
            ejectItems.Act = () => TryEjectAllItems(component, args.User);
            ejectItems.Text = Loc.GetString("pneumatic-cannon-component-verb-eject-items-name");
            args.Verbs.Add(ejectItems);
        }

        public void TryEjectAllItems(PitchingMachineComponent component, EntityUid user)
        {
            if (!TryComp<ServerStorageComponent?>(component.Owner, out var storage))
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
