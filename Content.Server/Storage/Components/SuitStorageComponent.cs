using System;
using System.Collections.Generic;
using Content.Server.CharacterAppearance.Components;
using Content.Server.EUI;
using Content.Server.Mind.Components;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Server.Storage;
using Content.Shared.Storage;
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
        protected override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent<PlaceableSurfaceComponent>(out var surface))
            {
                EntitySystem.Get<PlaceableSurfaceSystem>().SetPlaceable(Owner.Uid, Open, surface);
            }

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

            return Contents.CanInsert(entity) && Insert(entity);
        }

        public bool CanInsert(IEntity toinsert)
        {
            DebugTools.Assert(!Deleted);

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

            // Crucial, prevent circular insertion.
            return !toinsert.Transform.ContainsEntity(Owner.Transform);

            //Improvement: Traverse the entire tree to make sure we are not creating a loop.
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

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
