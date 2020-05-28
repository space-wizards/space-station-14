using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Audio;
using Content.Shared.GameObjects;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class StunbatonComponent : MeleeWeaponComponent, IUse, IExamine, IMapInit, IInteractUsing
    {
#pragma warning disable 649
        [Dependency] private IRobustRandom _robustRandom;
        [Dependency] private IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly ISharedNotifyManager _notifyManager;
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649

        public override string Name => "Stunbaton";

        private bool _activated = false;

        [ViewVariables] private ContainerSlot _cellContainer;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _paralyzeChance = 0.25f;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _paralyzeTime = 10f;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _slowdownTime = 5f;

        [ViewVariables(VVAccess.ReadWrite)] public float EnergyPerUse { get; set; } = 1000;

        [ViewVariables]
        public bool Activated => _activated;

        [ViewVariables]
        private PowerCellComponent Cell
        {
            get
            {
                if (_cellContainer.ContainedEntity == null) return null;

                _cellContainer.ContainedEntity.TryGetComponent(out PowerCellComponent cell);
                return cell;
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            _cellContainer =
                ContainerManagerComponent.Ensure<ContainerSlot>("stunbaton_cell_container", Owner, out var existed);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _paralyzeChance, "paralyzeChance", 0.25f);
            serializer.DataField(ref _paralyzeTime, "paralyzeTime", 10f);
            serializer.DataField(ref _slowdownTime, "slowdownTime", 5f);
        }

        public override bool OnHitEntities(IReadOnlyList<IEntity> entities)
        {
            var cell = Cell;
            if (!Activated || entities.Count == 0 || cell == null || !cell.CanDeductCharge(EnergyPerUse))
                return false;

            _entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/weapons/egloves.ogg", Owner.Transform.GridPosition, AudioHelpers.WithVariation(0.25f));

            foreach (var entity in entities)
            {
                if (!entity.TryGetComponent(out StunnableComponent stunnable)) continue;

                if(_robustRandom.Prob(_paralyzeChance))
                    stunnable.Paralyze(_paralyzeTime);
                else
                    stunnable.Slowdown(_slowdownTime);
            }

            cell.DeductCharge(EnergyPerUse);
            if(cell.Charge < EnergyPerUse)
            {
                _entitySystemManager.GetEntitySystem<AudioSystem>().Play(AudioHelpers.GetRandomFileFromSoundCollection("sparks"), Owner.Transform.GridPosition, AudioHelpers.WithVariation(0.25f));
                TurnOff();
            }

            return false;
        }

        private bool ToggleStatus(IEntity user)
        {
            if (Activated)
            {
                TurnOff();
            }
            else
            {
                TurnOn(user);
            }

            return true;
        }

        private void TurnOff()
        {
            if (!_activated)
            {
                return;
            }

            var sprite = Owner.GetComponent<SpriteComponent>();
            var item = Owner.GetComponent<ItemComponent>();

            _entitySystemManager.GetEntitySystem<AudioSystem>().Play(AudioHelpers.GetRandomFileFromSoundCollection("sparks"), Owner.Transform.GridPosition, AudioHelpers.WithVariation(0.25f));

            item.EquippedPrefix = "off";
            sprite.LayerSetState(0, "stunbaton_off");
            _activated = false;
        }

        private void TurnOn(IEntity user)
        {
            if (_activated)
            {
                return;
            }

            var sprite = Owner.GetComponent<SpriteComponent>();
            var item = Owner.GetComponent<ItemComponent>();
            var cell = Cell;

            if (cell == null)
            {
                _entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/machines/button.ogg", Owner.Transform.GridPosition, AudioHelpers.WithVariation(0.25f));

                _notifyManager.PopupMessage(Owner, user, _localizationManager.GetString("Cell missing..."));
                return;
            }

            if (cell.Charge < EnergyPerUse)
            {
                _entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/machines/button.ogg", Owner.Transform.GridPosition, AudioHelpers.WithVariation(0.25f));
                _notifyManager.PopupMessage(Owner, user, _localizationManager.GetString("Dead cell..."));
                return;
            }

            _entitySystemManager.GetEntitySystem<AudioSystem>().Play(AudioHelpers.GetRandomFileFromSoundCollection("sparks"), Owner.Transform.GridPosition, AudioHelpers.WithVariation(0.25f));

            item.EquippedPrefix = "on";
            sprite.LayerSetState(0, "stunbaton_on");
            _activated = true;
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            ToggleStatus(eventArgs.User);

            return true;
        }

        public bool InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.HasComponent<PowerCellComponent>()) return false;

            if (Cell != null) return false;

            var handsComponent = eventArgs.User.GetComponent<IHandsComponent>();

            if (!handsComponent.Drop(eventArgs.Using, _cellContainer))
            {
                return false;
            }

            _entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/items/weapons/pistol_magin.ogg");

            Dirty();

            return true;
        }

        private void EjectCell(IEntity user)
        {
            if (Cell == null)
            {
                return;
            }

            var cell = Cell;

            if (!_cellContainer.Remove(cell.Owner))
            {
                return;
            }

            if (!user.TryGetComponent(out HandsComponent hands))
            {
                return;
            }

            if (!hands.PutInHand(cell.Owner.GetComponent<ItemComponent>()))
            {
                cell.Owner.Transform.GridPosition = user.Transform.GridPosition;
            }

            _entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/items/weapons/pistol_magout.ogg", Owner.Transform.GridPosition, AudioHelpers.WithVariation(0.25f));
        }

        public void Examine(FormattedMessage message)
        {
            var loc = IoCManager.Resolve<ILocalizationManager>();

            if (Activated)
            {
                message.AddMarkup(loc.GetString("The light is currently [color=darkgreen]on[/color]."));
            }
        }

        public void MapInit()
        {
            if (_cellContainer.ContainedEntity != null)
            {
                return;
            }

            var cell = Owner.EntityManager.SpawnEntity("PowerCellSmallHyper", Owner.Transform.GridPosition);
            _cellContainer.Insert(cell);
        }

        [Verb]
        public sealed class EjectCellVerb : Verb<StunbatonComponent>
        {
            protected override void GetData(IEntity user, StunbatonComponent component, VerbData data)
            {
                if (component.Cell == null)
                {
                    data.Text = "Eject cell (cell missing)";
                    data.Visibility = VerbVisibility.Disabled;
                }
                else
                {
                    data.Text = "Eject cell";
                }
            }

            protected override void Activate(IEntity user, StunbatonComponent component)
            {
                component.EjectCell(user);
            }
        }
    }
}
