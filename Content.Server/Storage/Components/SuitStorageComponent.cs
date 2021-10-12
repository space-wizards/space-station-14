using System;
using System.Linq;
using System.Collections.Generic;
using Content.Server.CharacterAppearance.Components;
using Content.Server.EUI;
using Content.Server.Mind.Components;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Server.Storage;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Content.Shared.MobState;
using Content.Shared.Sound;
using Content.Shared.Popups;
using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Maths;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using Content.Shared.Placeable;

namespace Content.Server.Storage.Components
{
    [RegisterComponent]
    public class SuitStorageComponent : SharedSuitStorageComponent
    {
        public TimeSpan LastInternalOpenAttempt;
        private const int OpenMask = (int) (
            CollisionGroup.MobImpassable |
            CollisionGroup.VaultImpassable |
            CollisionGroup.SmallImpassable);

        private const float MaxSize = 1.0f;
        private Dictionary<int, IEntity> ContentsLookup = new Dictionary<int, IEntity>();

        [ViewVariables]
        [DataField("IsCollidableWhenOpen")]
        private bool _isCollidableWhenOpen;

        [DataField("open")]
        private bool _open;

        [DataField("CanWeldShut")]
        private bool _canWeldShut = true;

        [DataField("IsWeldedShut")]
        private bool _isWeldedShut;

        [DataField("closeSound")]
        private SoundSpecifier _closeSound = new SoundPathSpecifier("/Audio/Effects/closetclose.ogg");

        [DataField("openSound")]
        private SoundSpecifier _openSound = new SoundPathSpecifier("/Audio/Effects/closetopen.ogg");

        [DataField("whitelist")]
        private EntityWhitelist? _whitelist = null;

        [ViewVariables]
        public bool Powered => !Owner.TryGetComponent(out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables]
        public BoundUserInterface? UserInterface =>
            Owner.GetUIOrNull(SuitStorageUIKey.Key);

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Open
        {
            get => _open;
            private set => _open = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsWeldedShut
        {
            get => _isWeldedShut;
            set
            {
                if (_isWeldedShut == value) return;

                _isWeldedShut = value;
                UpdateAppearance();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanWeldShut
        {
            get => _canWeldShut;
            set
            {
                if (_canWeldShut == value) return;

                _canWeldShut = value;
                UpdateAppearance();
            }
        }

        [ViewVariables]
        public Container Contents = default!;

        [ViewVariables]
        public bool UiKnownPowerState = false;
        protected override void Initialize()
        {
            base.Initialize();

            Contents = Owner.EnsureContainer<Container>(nameof(EntityStorageComponent));

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }
        }

        public void ToggleOpen(IEntity user)
        {
            if (Open)
            {
                TryCloseStorage(user);
            }
            else
            {
                TryOpenStorage(user);
            }
        }

        public bool TryOpenStorage(IEntity user)
        {
            if (!CanOpen()) return false;
            OpenStorage();
            return true;
        }

        public bool TryCloseStorage(IEntity user)
        {
            if (!CanClose()) return false;
            CloseStorage();
            return true;
        }

        public bool CanOpen()
        {
            if (IsWeldedShut)
            {
                return false;
            }

            if (Owner.TryGetComponent<LockComponent>(out var @lock) && @lock.Locked)
            {
                return false;
            }

            return true;
        }

        public bool CanClose()
        {
            return true;
        }

        protected void CloseStorage()
        {
            Open = false;

            ModifyComponents();
            SoundSystem.Play(Filter.Pvs(Owner), _closeSound.GetSound(), Owner);
            LastInternalOpenAttempt = default;
        }

        protected void OpenStorage()
        {
            Open = true;
            ModifyComponents();
            SoundSystem.Play(Filter.Pvs(Owner), _openSound.GetSound(), Owner);
        }

        private void UpdateAppearance()
        {
            if (Owner.TryGetComponent<PlaceableSurfaceComponent>(out var surface))
            {
                EntitySystem.Get<PlaceableSurfaceSystem>().SetPlaceable(Owner.Uid, Open, surface);
            }

            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(SuitStorageVisuals.Open, Open);
            }
        }

        private void UpdateContentsLookup()
        {
            ContentsLookup.Clear();
            int index = 0;
            foreach (var item in Contents.ContainedEntities)
            {
                ContentsLookup.Add(index, item);
                index++;
            }
        }

        public Dictionary<int, string?> ContainedItemNameLookup()
        {
            return ContentsLookup.ToDictionary(m => m.Key, m => (string?)m.Value.Name);
        }

        protected IEnumerable<IEntity> DetermineCollidingEntities()
        {
            var entityLookup = IoCManager.Resolve<IEntityLookup>();
            return entityLookup.GetEntitiesIntersecting(Owner);
        }

        private void ModifyComponents()
        {
            if (!_isCollidableWhenOpen && Owner.TryGetComponent<IPhysBody>(out var physics))
            {
                if (Open)
                {
                    foreach (var fixture in physics.Fixtures)
                    {
                        fixture.CollisionLayer &= ~OpenMask;
                    }
                }
                else
                {
                    foreach (var fixture in physics.Fixtures)
                    {
                        fixture.CollisionLayer |= OpenMask;
                    }
                }
            }

            UpdateAppearance();
        }

        public bool Insert(IEntity entity)
        {
            if (!Contents.Insert(entity)) return false;

            entity.Transform.LocalPosition = Vector2.Zero;
            if (entity.TryGetComponent(out IPhysBody? body))
            {
                body.CanCollide = false;
            }

            UpdateUserInterface();

            return true;
        }

        public bool AddToContents(IEntity entity)
        {
            if (entity == Owner) return false;
            if (entity.TryGetComponent(out PhysicsComponent? entityPhysicsComponent))
            {
                if (MaxSize < entityPhysicsComponent.GetWorldAABB().Size.X
                    || MaxSize < entityPhysicsComponent.GetWorldAABB().Size.Y)
                {
                    return false;
                }
            }

            return CanInsert(entity) && Insert(entity);
        }

        public bool CanInsert(IEntity toinsert)
        {
            DebugTools.Assert(!Deleted);

            if(!Open){
                return false;
            }

            // cannot insert into itself.
            if (Owner == toinsert)
                return false;

            // no, you can't put maps or grids into containers
            if (toinsert.HasComponent<IMapComponent>() || toinsert.HasComponent<IMapGridComponent>())
                return false;

            if (_whitelist != null && !_whitelist.IsValid(toinsert))
            {
                return false;
            }

            if(CheckForDoubles(toinsert)){
                return false;
            }

            // Crucial, prevent circular insertion.
            return !toinsert.Transform.ContainsEntity(Owner.Transform);

            //Improvement: Traverse the entire tree to make sure we are not creating a loop.
        }

        private bool CheckForDoubles(IEntity toinsert)
        {
            IReadOnlyList<IEntity> containedEntities = Contents.ContainedEntities;
            if(_whitelist == null) return false;
            string[]? whitelistTags = _whitelist.Tags;
            if(whitelistTags == null) return false;

            foreach (string tag in whitelistTags)
            {
                foreach (IEntity item in containedEntities)
                {
                    if(item.HasTag(tag) && toinsert.HasTag(tag)) return true;
                }
            }
            return false;
        }

        private void DispenseItem(int index)
        {
            ContentsLookup.TryGetValue(index, out IEntity? contained);

            if (contained != null && Contents.Remove(contained))
            {
                contained.Transform.WorldPosition = ContentsDumpPosition();
                if (contained.TryGetComponent<IPhysBody>(out var physics))
                {
                    physics.CanCollide = true;
                }

                UpdateUserInterface();
            }
        }

        private bool CanDispense(int index)
        {
            if(Open)
            {
                return true;
            }

            return false;
        }

        public virtual Vector2 ContentsDumpPosition()
        {
            return Owner.Transform.WorldPosition;
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Message is not SuitStorageUiButtonPressedMessage message) return;

            switch (message.Button)
            {
                case UiButton.Open:
                    if(CanOpen()) OpenStorage();
                    break;

                case UiButton.Close:
                    if(CanClose()) CloseStorage();
                    break;
                case UiButton.Dispense:
                    if(message.ItemId != null && obj.Session.AttachedEntityUid != null && CanDispense((int)message.ItemId)){
                        DispenseItem((int)message.ItemId);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UpdateUserInterface()
        {
            UpdateContentsLookup();
            EntitySystem.Get<SuitStorageSystem>().UpdateUserInterface(this);
        }
    }
}
