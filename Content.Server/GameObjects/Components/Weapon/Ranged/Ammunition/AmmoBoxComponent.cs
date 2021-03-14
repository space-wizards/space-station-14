using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Weapon.Ranged.Barrels;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition
{
    [RegisterComponent]
    public sealed class AmmoBoxComponent : Component, IInteractUsing, IUse, IInteractHand, IMapInit, IExamine
    {
        public override string Name => "AmmoBox";

        [DataField("caliber")]
        private BallisticCaliber _caliber = BallisticCaliber.Unspecified;

        [DataField("capacity")]
        public int Capacity
        {
            get => _capacity;
            set
            {
                _capacity = value;
                _spawnedAmmo = new Stack<IEntity>(value);
            }
        }

        private int _capacity = 30;

        public int AmmoLeft => _spawnedAmmo.Count + _unspawnedCount;
        private Stack<IEntity> _spawnedAmmo = new();
        private Container _ammoContainer;
        private int _unspawnedCount;

        [DataField("fillPrototype")]
        private string _fillPrototype;

        public override void Initialize()
        {
            base.Initialize();
            _ammoContainer = ContainerHelpers.EnsureContainer<Container>(Owner, $"{Name}-container", out var existing);

            if (existing)
            {
                foreach (var entity in _ammoContainer.ContainedEntities)
                {
                    _unspawnedCount--;
                    _spawnedAmmo.Push(entity);
                    _ammoContainer.Insert(entity);
                }
            }

        }

        void IMapInit.MapInit()
        {
            _unspawnedCount += _capacity;
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearanceComponent))
            {
                appearanceComponent.SetData(MagazineBarrelVisuals.MagLoaded, true);
                appearanceComponent.SetData(AmmoVisuals.AmmoCount, AmmoLeft);
                appearanceComponent.SetData(AmmoVisuals.AmmoMax, _capacity);
            }
        }

        public IEntity TakeAmmo()
        {
            if (_spawnedAmmo.TryPop(out IEntity ammo))
            {
                _ammoContainer.Remove(ammo);
                return ammo;
            }

            if (_unspawnedCount > 0)
            {
                ammo = Owner.EntityManager.SpawnEntity(_fillPrototype, Owner.Transform.Coordinates);
                _unspawnedCount--;
            }

            return ammo;
        }

        public bool TryInsertAmmo(IEntity user, IEntity entity)
        {
            if (!entity.TryGetComponent(out AmmoComponent ammoComponent))
            {
                return false;
            }

            if (ammoComponent.Caliber != _caliber)
            {
                Owner.PopupMessage(user, Loc.GetString("Wrong caliber"));
                return false;
            }

            if (AmmoLeft >= Capacity)
            {
                Owner.PopupMessage(user, Loc.GetString("No room"));
                return false;
            }

            _spawnedAmmo.Push(entity);
            _ammoContainer.Insert(entity);
            UpdateAppearance();
            return true;
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (eventArgs.Using.HasComponent<AmmoComponent>())
            {
                return TryInsertAmmo(eventArgs.User, eventArgs.Using);
            }

            if (eventArgs.Using.TryGetComponent(out RangedMagazineComponent rangedMagazine))
            {
                for (var i = 0; i < Math.Max(10, rangedMagazine.ShotsLeft); i++)
                {
                    var ammo = rangedMagazine.TakeAmmo();

                    if (!TryInsertAmmo(eventArgs.User, ammo))
                    {
                        rangedMagazine.TryInsertAmmo(eventArgs.User, ammo);
                        return true;
                    }
                }

                return true;
            }

            return false;
        }

        private bool TryUse(IEntity user)
        {
            if (!user.TryGetComponent(out HandsComponent handsComponent))
            {
                return false;
            }

            var ammo = TakeAmmo();
            var itemComponent = ammo.GetComponent<ItemComponent>();

            if (!handsComponent.CanPutInHand(itemComponent))
            {
                TryInsertAmmo(user, ammo);
                return false;
            }

            handsComponent.PutInHand(itemComponent);
            UpdateAppearance();
            return true;
        }

        private void EjectContents(int count)
        {
            var ejectCount = Math.Min(count, Capacity);
            var ejectAmmo = new List<IEntity>(ejectCount);

            for (var i = 0; i < Math.Min(count, Capacity); i++)
            {
                var ammo = TakeAmmo();
                if (ammo == null)
                {
                    break;
                }

                ejectAmmo.Add(ammo);
            }

            ServerRangedBarrelComponent.EjectCasings(ejectAmmo);
            UpdateAppearance();
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return TryUse(eventArgs.User);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return TryUse(eventArgs.User);
        }



        // So if you have 200 rounds in a box and that suddenly creates 200 entities you're not having a fun time
        [Verb]
        private sealed class DumpVerb : Verb<AmmoBoxComponent>
        {
            protected override void GetData(IEntity user, AmmoBoxComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Dump 10");
                data.Visibility = component.AmmoLeft > 0 ? VerbVisibility.Visible : VerbVisibility.Disabled;
                data.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.96dpi.png";
            }

            protected override void Activate(IEntity user, AmmoBoxComponent component)
            {
                component.EjectContents(10);
            }
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("\nIt's a [color=white]{0}[/color] ammo box.", _caliber));
            message.AddMarkup(Loc.GetString("\nIt has [color=white]{0}[/color] out of [color=white]{1}[/color] ammo left.", AmmoLeft, _capacity));
        }
    }
}
