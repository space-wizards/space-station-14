using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Power;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.Audio;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
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
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override string Name => "Stunbaton";

        private bool _activated = false;

        [ViewVariables] private ContainerSlot _cellContainer;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _paralyzeChanceNoSlowdown = 0.35f;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _paralyzeChanceWithSlowdown = 0.85f;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _paralyzeTime = 10f;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _slowdownTime = 5f;

        [ViewVariables(VVAccess.ReadWrite)] public float EnergyPerUse { get; set; } = 50;

        [ViewVariables]
        public bool Activated => _activated;

        [ViewVariables]
        private BatteryComponent Cell
        {
            get
            {
                if (_cellContainer.ContainedEntity == null) return null;

                _cellContainer.ContainedEntity.TryGetComponent(out BatteryComponent cell);
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

            serializer.DataField(ref _paralyzeChanceNoSlowdown, "paralyzeChanceNoSlowdown", 0.35f);
            serializer.DataField(ref _paralyzeChanceWithSlowdown, "paralyzeChanceWithSlowdown", 0.85f);
            serializer.DataField(ref _paralyzeTime, "paralyzeTime", 10f);
            serializer.DataField(ref _slowdownTime, "slowdownTime", 5f);
        }

        protected override bool OnHitEntities(IReadOnlyList<IEntity> entities, AttackEventArgs eventArgs)
        {
            if (!Activated || entities.Count == 0 || Cell == null)
                return true;

            if (!Cell.TryUseCharge(EnergyPerUse))
                return true;

            EntitySystem.Get<AudioSystem>().PlayAtCoords("/Audio/Weapons/egloves.ogg", Owner.Transform.GridPosition, AudioHelpers.WithVariation(0.25f));

            foreach (var entity in entities)
            {
                if (!entity.TryGetComponent(out StunnableComponent stunnable)) continue;

                if(!stunnable.SlowedDown)
                    if(_robustRandom.Prob(_paralyzeChanceNoSlowdown))
                        stunnable.Paralyze(_paralyzeTime);
                    else
                        stunnable.Slowdown(_slowdownTime);
                else
                    if(_robustRandom.Prob(_paralyzeChanceWithSlowdown))
                        stunnable.Paralyze(_paralyzeTime);
                    else
                        stunnable.Slowdown(_slowdownTime);
            }

            if (!(Cell.CurrentCharge < EnergyPerUse)) return true;

            EntitySystem.Get<AudioSystem>().PlayAtCoords(AudioHelpers.GetRandomFileFromSoundCollection("sparks"), Owner.Transform.GridPosition, AudioHelpers.WithVariation(0.25f));
            TurnOff();

            return true;
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

            EntitySystem.Get<AudioSystem>().PlayAtCoords(AudioHelpers.GetRandomFileFromSoundCollection("sparks"), Owner.Transform.GridPosition, AudioHelpers.WithVariation(0.25f));

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
                EntitySystem.Get<AudioSystem>().PlayAtCoords("/Audio/Machines/button.ogg", Owner.Transform.GridPosition, AudioHelpers.WithVariation(0.25f));

                Owner.PopupMessage(user, Loc.GetString("Cell missing..."));
                return;
            }

            if (cell.CurrentCharge < EnergyPerUse)
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords("/Audio/Machines/button.ogg", Owner.Transform.GridPosition, AudioHelpers.WithVariation(0.25f));
                Owner.PopupMessage(user, Loc.GetString("Dead cell..."));
                return;
            }

            EntitySystem.Get<AudioSystem>().PlayAtCoords(AudioHelpers.GetRandomFileFromSoundCollection("sparks"), Owner.Transform.GridPosition, AudioHelpers.WithVariation(0.25f));

            item.EquippedPrefix = "on";
            sprite.LayerSetState(0, "stunbaton_on");
            _activated = true;
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            ToggleStatus(eventArgs.User);

            return true;
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.HasComponent<BatteryComponent>()) return false;

            if (Cell != null) return false;

            var handsComponent = eventArgs.User.GetComponent<IHandsComponent>();

            if (!handsComponent.Drop(eventArgs.Using, _cellContainer))
            {
                return false;
            }

            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Items/pistol_magin.ogg", Owner);

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

            EntitySystem.Get<AudioSystem>().PlayAtCoords("/Audio/Items/pistol_magout.ogg", Owner.Transform.GridPosition, AudioHelpers.WithVariation(0.25f));
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (Activated)
            {
                message.AddMarkup(Loc.GetString("The light is currently [color=darkgreen]on[/color]."));
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
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (component.Cell == null)
                {
                    data.Text = Loc.GetString("Eject cell (cell missing)");
                    data.Visibility = VerbVisibility.Disabled;
                }
                else
                {
                    data.Text = Loc.GetString("Eject cell");
                }
            }

            protected override void Activate(IEntity user, StunbatonComponent component)
            {
                component.EjectCell(user);
            }
        }
    }
}
