#nullable enable
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Morgue;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Morgue
{
    [RegisterComponent]
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class MorgueEntityStorageComponent : EntityStorageComponent, IExamine
    {
        public override string Name => "MorgueEntityStorage";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("trayPrototype")]
        private string? _trayPrototypeId;

        [ViewVariables]
        private IEntity? _tray;

        [ViewVariables]
        public ContainerSlot? TrayContainer { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("doSoulBeep")]
        public bool DoSoulBeep = true;

        [ViewVariables]
        [ComponentDependency] protected readonly AppearanceComponent? Appearance = null;


        public override void Initialize()
        {
            base.Initialize();
            Appearance?.SetData(MorgueVisuals.Open, false);
            TrayContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, "morgue_tray", out _);
        }

        public override Vector2 ContentsDumpPosition()
        {
            if (_tray != null) return _tray.Transform.WorldPosition;
            return base.ContentsDumpPosition();
        }

        protected override bool AddToContents(IEntity entity)
        {
            if (entity.HasComponent<IBody>() && !EntitySystem.Get<StandingStateSystem>().IsDown(entity)) return false;
            return base.AddToContents(entity);
        }

        public override bool CanOpen(IEntity user, bool silent = false)
        {
            if (!Owner.InRangeUnobstructed(
                Owner.Transform.Coordinates.Offset(Owner.Transform.LocalRotation.GetCardinalDir()),
                collisionMask: CollisionGroup.Impassable | CollisionGroup.VaultImpassable
            ))
            {
                if(!silent) Owner.PopupMessage(user, Loc.GetString("There's no room for the tray to extend!"));
                return false;
            }

            return base.CanOpen(user, silent);
        }

        protected override void OpenStorage()
        {
            Appearance?.SetData(MorgueVisuals.Open, true);
            Appearance?.SetData(MorgueVisuals.HasContents, false);
            Appearance?.SetData(MorgueVisuals.HasMob, false);
            Appearance?.SetData(MorgueVisuals.HasSoul, false);

            if (_tray == null)
            {
                _tray = Owner.EntityManager.SpawnEntity(_trayPrototypeId, Owner.Transform.Coordinates);
                var trayComp = _tray.EnsureComponent<MorgueTrayComponent>();
                trayComp.Morgue = Owner;
                EntityQuery = new IntersectingEntityQuery(_tray);
            }
            else
            {
                TrayContainer?.Remove(_tray);
            }

            _tray.Transform.WorldPosition = Owner.Transform.WorldPosition + Owner.Transform.LocalRotation.GetCardinalDir().ToVec();
            _tray.Transform.AttachParent(Owner);

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
                if (!hasMob && entity.HasComponent<IBody>()) hasMob = true;
                if (!hasSoul && entity.TryGetComponent<ActorComponent>(out var actor) && actor.PlayerSession != null) hasSoul = true;
            }
            Appearance?.SetData(MorgueVisuals.HasContents, count > 0);
            Appearance?.SetData(MorgueVisuals.HasMob, hasMob);
            Appearance?.SetData(MorgueVisuals.HasSoul, hasSoul);
        }

        protected override void CloseStorage()
        {
            base.CloseStorage();

            Appearance?.SetData(MorgueVisuals.Open, false);
            CheckContents();

            if (_tray != null)
            {
                TrayContainer?.Insert(_tray);
            }
        }

        //Called every 10 seconds
        public void Update()
        {
            CheckContents();

            if(DoSoulBeep && Appearance !=null && Appearance.TryGetData(MorgueVisuals.HasSoul, out bool hasSoul) && hasSoul)
                SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg", Owner);
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (Appearance == null) return;

            if (inDetailsRange)
            {
                if (Appearance.TryGetData(MorgueVisuals.HasSoul, out bool hasSoul) && hasSoul)
                {
                    message.AddMarkup(Loc.GetString("The content light is [color=green]green[/color], this body might still be saved!"));
                }
                else if (Appearance.TryGetData(MorgueVisuals.HasMob, out bool hasMob) && hasMob)
                {
                    message.AddMarkup(Loc.GetString("The content light is [color=red]red[/color], there's a dead body in here! Oh wait..."));
                }
                else if (Appearance.TryGetData(MorgueVisuals.HasContents, out bool hasContents) && hasContents)
                {
                    message.AddMarkup(Loc.GetString("The content light is [color=yellow]yellow[/color], there's something in here."));
                } else
                {
                    message.AddMarkup(Loc.GetString("The content light is off, there's nothing in here."));
                }
            }
        }
    }
}
