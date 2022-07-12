using Content.Server.Storage.Components;
using Content.Shared.Body.Components;
using Content.Shared.Directions;
using Content.Shared.Interaction;
using Content.Shared.Morgue;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Standing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Morgue.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    [Virtual]
    public class MorgueEntityStorageComponent : EntityStorageComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        private const CollisionGroup TrayCanOpenMask = CollisionGroup.Impassable | CollisionGroup.MidImpassable;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("trayPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        private string? _trayPrototypeId;

        [ViewVariables]
        private EntityUid? _tray;

        [ViewVariables]
        public ContainerSlot? TrayContainer { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("doSoulBeep")]
        public bool DoSoulBeep = true;

        [DataField("occupantHasSoulAlarmSound")]
        private SoundSpecifier _occupantHasSoulAlarmSound = new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg");

        protected override void Initialize()
        {
            base.Initialize();
            if(_entMan.TryGetComponent<AppearanceComponent>(Owner, out var appearance))
                appearance.SetData(MorgueVisuals.Open, false);
            TrayContainer = Owner.EnsureContainer<ContainerSlot>("morgue_tray", out _);
        }

        public override Vector2 ContentsDumpPosition()
        {
            if (_tray != null)
                return _entMan.GetComponent<TransformComponent>(_tray.Value).WorldPosition;
            return base.ContentsDumpPosition();
        }

        protected override bool AddToContents(EntityUid entity)
        {
            if (_entMan.HasComponent<SharedBodyComponent>(entity) && !EntitySystem.Get<StandingStateSystem>().IsDown(entity))
                return false;
            return base.AddToContents(entity);
        }

        public override bool CanOpen(EntityUid user, bool silent = false)
        {
            if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(Owner,
                _entMan.GetComponent<TransformComponent>(Owner).Coordinates.Offset(_entMan.GetComponent<TransformComponent>(Owner).LocalRotation.GetCardinalDir()),
                collisionMask: TrayCanOpenMask
            ))
            {
                if (!silent)
                    Owner.PopupMessage(user, Loc.GetString("morgue-entity-storage-component-cannot-open-no-space"));
                return false;
            }

            return base.CanOpen(user, silent);
        }

        protected override void OpenStorage()
        {
            if (_entMan.TryGetComponent<AppearanceComponent>(Owner, out var appearance))
            {
                appearance.SetData(MorgueVisuals.Open, true);
                appearance.SetData(MorgueVisuals.HasContents, false);
                appearance.SetData(MorgueVisuals.HasMob, false);
                appearance.SetData(MorgueVisuals.HasSoul, false);
            }

            if (_tray == null)
            {
                _tray = _entMan.SpawnEntity(_trayPrototypeId, _entMan.GetComponent<TransformComponent>(Owner).Coordinates);
                var trayComp = _tray.Value.EnsureComponent<MorgueTrayComponent>();
                trayComp.Morgue = Owner;
            }
            else
            {
                TrayContainer?.Remove(_tray.Value);
            }

            _entMan.GetComponent<TransformComponent>(_tray.Value).Coordinates = new EntityCoordinates(Owner, 0, -1);

            base.OpenStorage();
        }

        private void CheckContents()
        {
            var count = 0;
            var hasMob = false;
            var hasSoul = false;
            foreach (var entity in Contents.ContainedEntities)
            {
                count++;
                if (!hasMob && _entMan.HasComponent<SharedBodyComponent>(entity))
                    hasMob = true;
                if (!hasSoul && _entMan.TryGetComponent<ActorComponent?>(entity, out var actor) && actor.PlayerSession != null)
                    hasSoul = true;
            }

            if (_entMan.TryGetComponent<AppearanceComponent>(Owner, out var appearance))
            {
                appearance.SetData(MorgueVisuals.HasContents, count > 0);
                appearance.SetData(MorgueVisuals.HasMob, hasMob);
                appearance.SetData(MorgueVisuals.HasSoul, hasSoul);
            }
        }

        protected override void CloseStorage()
        {
            base.CloseStorage();

            if (_entMan.TryGetComponent<AppearanceComponent>(Owner, out var appearance))
                appearance.SetData(MorgueVisuals.Open, false);
            CheckContents();

            if (_tray != null)
            {
                TrayContainer?.Insert(_tray.Value);
            }
        }

        protected override IEnumerable<EntityUid> DetermineCollidingEntities()
        {
            if (_tray == null)
            {
                yield break;
            }

            var entityLookup = EntitySystem.Get<EntityLookupSystem>();
            foreach (var entity in entityLookup.GetEntitiesIntersecting(_tray.Value, flags: LookupFlags.None))
            {
                yield return entity;
            }
        }

        //Called every 10 seconds
        public void Update()
        {
            CheckContents();

            if (DoSoulBeep && _entMan.TryGetComponent<AppearanceComponent>(Owner, out var appearance) &&
                appearance.TryGetData(MorgueVisuals.HasSoul, out bool hasSoul) && hasSoul)
            {
                SoundSystem.Play(_occupantHasSoulAlarmSound.GetSound(), Filter.Pvs(Owner), Owner);
            }
        }
    }
}
