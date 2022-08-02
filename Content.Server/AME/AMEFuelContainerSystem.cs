using System.Linq;
using Content.Server.AME.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Item;
using Content.Server.Popups;
using Content.Server.Singularity.Components;
using Content.Server.Tools;
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
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        private readonly HashSet<EntityUid> _activeAMEFuelContainers = new();

        private const float AMEFuelContainerUpdateTimer = 1f;
        private float _AMEFuelContainerTimer = 0f;

        // A full (1000 unit) AME jar will feed the singulo by this amount
        private const int SinguloFoodPerThousand = -150;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AMEFuelContainerComponent, ComponentStartup>(OnAMEFuelContainerStartup);
            SubscribeLocalEvent<AMEFuelContainerComponent, ExaminedEvent>(OnAMEFuelContainerExamine);
            SubscribeLocalEvent<AMEFuelContainerComponent, InteractUsingEvent>(OnAMEFuelContainerInteractUsing);
            SubscribeLocalEvent<AMEFuelContainerComponent, ComponentShutdown>(OnAMEFuelContainerShutdown);
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
            PointLightComponent? light = null)
        {
            if (!Resolve(uid, ref container, ref singuloFood, ref light))
                return false;

            if (!container.Sealed)
                return false;

            Resolve(uid, ref item);

            container.Sealed = false;
            singuloFood.Energy = SinguloFoodPerThousand;
            light.Enabled = true;

            if (item != null)
                _itemSystem.SetHeldPrefix(uid, "open", item);

            SoundSystem.Play("/Audio/Items/crowbar.ogg", Filter.Pvs(uid, entityManager: EntityManager), uid);

            if(user != null)
                _popupSystem.PopupEntity(Loc.GetString("ame-fuel-container-component-on-pry"), uid, Filter.Entities(user.Value));

            container.Dirty();

            _activeAMEFuelContainers.Add(uid);
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

        private void OnAMEFuelContainerInteractUsing(EntityUid uid, AMEFuelContainerComponent container,
            InteractUsingEvent args)
        {
            if (!_toolSystem.HasQuality(args.Used, container.QualityNeeded))
                return;

            args.Handled = TryOpenAMEFuelContainer(uid, args.User, container);
        }

        private void OnAMEFuelContainerShutdown(EntityUid uid, AMEFuelContainerComponent container,
            ComponentShutdown args)
        {
            _activeAMEFuelContainers.Remove(uid);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _AMEFuelContainerTimer += frameTime;

            if (_AMEFuelContainerTimer < AMEFuelContainerUpdateTimer)
                return;

            foreach (var uid in _activeAMEFuelContainers.ToArray())
            {
                if (!EntityManager.TryGetComponent(uid, out AMEFuelContainerComponent? container)
                    || !EntityManager.TryGetComponent(uid, out SinguloFoodComponent? singuloFood)
                    || !EntityManager.TryGetComponent(uid, out PointLightComponent? light))
                    continue;

                container.FuelAmount -= (int)Math.Round(container.OpenFuelConsumption * _AMEFuelContainerTimer);

                // Finish emitting fuel
                if (container.FuelAmount <= 0)
                {
                    _activeAMEFuelContainers.Remove(uid);
                    container.FuelAmount = 0;
                    light.Enabled = false;
                }

                singuloFood.Energy = container.FuelAmount * SinguloFoodPerThousand / 1000;

                container.Dirty();
            }

            _AMEFuelContainerTimer -= AMEFuelContainerUpdateTimer;
        }
    }
}
