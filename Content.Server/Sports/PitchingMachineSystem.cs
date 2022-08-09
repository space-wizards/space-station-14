using Content.Server.Popups;
using Content.Server.Sports.Components;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;
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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PitchingMachineComponent, GetVerbsEvent<Verb>>(AddEjectVerb);
            SubscribeLocalEvent<PitchingMachineComponent, GetVerbsEvent<AlternativeVerb>>(AddPowerVerb);
            SubscribeLocalEvent<PitchingMachineComponent, InteractUsingEvent>(OnInteractUsing);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var gunQuery = GetEntityQuery<GunComponent>();

            foreach (var ballLauncher in EntityQuery<PitchingMachineComponent>())
            {
                ballLauncher.AccumulatedFrametime += frameTime;

                if (!gunQuery.TryGetComponent(ballLauncher.Owner, out var gunComponent))
                    continue;

                if (ballLauncher.AccumulatedFrametime < ballLauncher.CurrentLauncherCooldown)
                    continue;

                ballLauncher.AccumulatedFrametime -= ballLauncher.CurrentLauncherCooldown;
                ballLauncher.CurrentLauncherCooldown = gunComponent.FireRate;

                if (ballLauncher.IsOn)
                    Fire(ballLauncher.Owner);
            }
        }

        private void OnInteractUsing(EntityUid uid, PitchingMachineComponent component, InteractUsingEvent args)
        {
            if (!TryComp<BallisticAmmoProviderComponent>(component.Owner, out var ammoProviderComponent))
                return;

            ammoProviderComponent.Entities.Add(args.Used);
            ammoProviderComponent.Container.Insert(args.Used);
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
            if (!TryComp<BallisticAmmoProviderComponent>(component.Owner, out var ammoProviderComponent))
                return;
            if (ammoProviderComponent.Container.ContainedEntities.Count == 0 || ammoProviderComponent.Entities.Count == 0)
                return;

            var projectile = _robustRandom.Pick(ammoProviderComponent.Container.ContainedEntities);

            var dir = Comp<TransformComponent>(component.Owner).WorldRotation.ToWorldVec() * _robustRandom.NextFloat(component.ShootDistanceMin, component.ShootDistanceMax);

            ammoProviderComponent.Entities.Remove(projectile);
            ammoProviderComponent.Container.Remove(projectile);

            _audioSystem.Play(_audioSystem.GetSound(component.FireSound), Filter.Pvs(component.Owner), component.Owner, AudioParams.Default);

            _throwingSystem.TryThrow(projectile, dir, 10f); //using throw because guncode is hardcoded to absolutely yeet it, impossible to actually hit with a bat

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
            if (!TryComp<BallisticAmmoProviderComponent?>(component.Owner, out var ammoProviderComponent))
                return;
            if (ammoProviderComponent.Entities.Count == 0)
                return;

            foreach (var entity in ammoProviderComponent.Entities.ToArray())
            {
                ammoProviderComponent.Entities.Remove(entity);
                ammoProviderComponent.Container.Remove(entity);
            }
            _popupSystem.PopupEntity(Loc.GetString("pneumatic-cannon-component-ejected-all", ("cannon", (component.Owner))), user, Filter.Local());
        }

    }
}
