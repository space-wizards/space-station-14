using System.Collections.Generic;
using Content.Server.Storage.Components;
using Content.Shared.Body.Components;
using Content.Shared.Directions;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Morgue;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Standing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Morgue.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    [Virtual]
#pragma warning disable 618
    public class MorgueEntityStorageComponent : EntityStorageComponent, IExamine
#pragma warning restore 618
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

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
                collisionMask: CollisionGroup.Impassable | CollisionGroup.VaultImpassable
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

            var entityLookup = IoCManager.Resolve<IEntityLookup>();
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
                SoundSystem.Play(Filter.Pvs(Owner), _occupantHasSoulAlarmSound.GetSound(), Owner);
            }
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!_entMan.TryGetComponent<AppearanceComponent>(Owner, out var appearance)) return;

            if (inDetailsRange)
            {
                if (appearance.TryGetData(MorgueVisuals.HasSoul, out bool hasSoul) && hasSoul)
                {
                    message.AddMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-body-has-soul"));
                }
                else if (appearance.TryGetData(MorgueVisuals.HasMob, out bool hasMob) && hasMob)
                {
                    message.AddMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-body-has-no-soul"));
                }
                else if (appearance.TryGetData(MorgueVisuals.HasContents, out bool hasContents) && hasContents)
                {
                    message.AddMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-has-contents"));
                }
                else
                {
                    message.AddMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-empty"));
                }
            }
        }
    }
}
