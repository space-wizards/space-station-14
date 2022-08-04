using Content.Server.AME.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Item;
using Content.Server.Singularity.Components;
using Content.Server.StationEvents.Events;
using Content.Server.Tools;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Shared.AME;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.AME
{
    public sealed class AMEFuelContainerSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly ToolSystem _toolSystem = default!;
        [Dependency] private readonly ItemSystem _itemSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly ArtifactSystem _artifactSystem = default!;

        private const float UpdateCooldown = 1f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AMEFuelContainerComponent, ComponentStartup>(OnAMEFuelContainerStartup);
            SubscribeLocalEvent<AMEFuelContainerComponent, ExaminedEvent>(OnAMEFuelContainerExamine);
            SubscribeLocalEvent<AMEFuelContainerComponent, InteractUsingEvent>(OnAMEFuelContainerInteractUsing);
        }

        private (int fuel, int capacity) GetAmeFuelContainerFuelAndCapacity(EntityUid uid,
            AMEFuelContainerComponent? container = null)
        {
            return !Resolve(uid, ref container)
                ? (0, 0)
                : (container.FuelAmount, container.MaxFuelAmount);
        }

        private bool TryOpenAMEFuelContainer(EntityUid uid, EntityUid? user,
            AMEFuelContainerComponent? container = null,
            ItemComponent? item = null,
            SinguloFoodComponent? singuloFood = null,
            PointLightComponent? light = null,
            AppearanceComponent? appearance = null,
            ArtifactComponent? artifact = null)
        {
            if (!Resolve(uid, ref container, ref singuloFood, ref light, ref appearance))
                return false;

            if (!Resolve(uid, ref item, ref artifact))
                return false;

            if (!container.Sealed)
                return false;

            container.Sealed = false;
            singuloFood.Energy = container.SinguloFoodPerThousand;
            light.Enabled = true;
            artifact.IsSuppressed = false;
            _itemSystem.SetHeldPrefix(uid, "open", item);

            appearance.SetData(AMEFuelContainerVisuals.IsOpen, true);

            SoundSystem.Play("/Audio/Items/crowbar.ogg", Filter.Pvs(uid, entityManager: EntityManager), uid);

            AddComp<LeakingAMEFuelContainerComponent>(uid);

            return true;
        }

        private void OnAMEFuelContainerStartup(EntityUid uid, AMEFuelContainerComponent component,
            ComponentStartup args)
        {
            component.Dirty();
        }

        private void OnAMEFuelContainerExamine(EntityUid uid, AMEFuelContainerComponent container, ExaminedEvent args)
        {
            args.PushMarkup(container.Sealed
                ? Loc.GetString("ame-fuel-container-component-on-examine-closed-message")
                : Loc.GetString("ame-fuel-container-component-on-examine-opened-message"));

            if (!args.IsInDetailsRange)
                return;

            var (fuel, capacity) = GetAmeFuelContainerFuelAndCapacity(uid, container);

            args.PushMarkup(Loc.GetString("ame-fuel-container-component-on-examine-detailed-message",
                ("colorName", fuel < capacity / FixedPoint2.New(4f) ? "darkorange" : "orange"),
                ("fuelLeft", fuel),
                ("fuelCapacity", capacity),
                ("status", string.Empty)));
        }

        private async void OnAMEFuelContainerInteractUsing(EntityUid uid, AMEFuelContainerComponent container,
            InteractUsingEvent args)
        {
            if (args.Handled || !await _toolSystem.UseTool(args.Used, args.User, uid, 0, 0, container.QualityNeeded))
                return;

            args.Handled = TryOpenAMEFuelContainer(uid, args.User, container);
        }

        private bool ActivateTemperatureArtifact(EntityUid uid,
            ArtifactComponent? artifact = null,
            TemperatureArtifactComponent? temperatureArtifact = null)
        {
            if (!Resolve(uid, ref artifact, ref temperatureArtifact))
                return false;

            if (!_artifactSystem.TryActivateArtifact(uid, component: artifact))
                return false;

            return true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (leaking, container, singuloFood, pointLight) in EntityQuery<LeakingAMEFuelContainerComponent, AMEFuelContainerComponent, SinguloFoodComponent, PointLightComponent>())
            {

                leaking.Accumulator += frameTime;

                if (leaking.Accumulator < UpdateCooldown)
                    continue;

                leaking.Accumulator -= UpdateCooldown;

                container.FuelAmount -= (int)Math.Round(container.OpenFuelConsumption * UpdateCooldown);

                ActivateTemperatureArtifact(container.Owner);

                // Finish emitting fuel
                if (container.FuelAmount <= 0)
                {
                    RemComp<LeakingAMEFuelContainerComponent>(container.Owner);
                    container.FuelAmount = 0;
                    pointLight.Enabled = false;
                }

                singuloFood.Energy = container.FuelAmount * container.SinguloFoodPerThousand / 1000;

                container.Dirty();
            }
        }
    }
}
