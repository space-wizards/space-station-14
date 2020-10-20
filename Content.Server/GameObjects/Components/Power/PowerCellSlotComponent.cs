#nullable enable
using System;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Provides a "battery compartment" that can contain a <see cref="PowerCellComponent"/> of the matching
    /// <see cref="PowerCellSize"/>. Intended to supplement other components, not very useful by itself.
    /// </summary>
    [RegisterComponent]
    public class PowerCellSlotComponent : Component, IMapInit
    {
        public override string Name => "PowerCellSlot";

        /// <summary>
        /// What size of cell fits into this component.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] public PowerCellSize SlotSize = PowerCellSize.Small;

        /// <summary>
        /// Can the cell be removed using the verb (or possibly other means)?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] public bool CanRemoveCell = true;

        /// <summary>
        /// File path to a sound file that should be played when the cell is removed.
        /// </summary>
        /// <example>"/Audio/Items/pistol_magout.ogg"</example>
        [ViewVariables(VVAccess.ReadWrite)] public string? CellRemoveSound = null;
        /// <summary>
        /// File path to a sound file that should be played when a cell is inserted.
        /// </summary>
        /// <example>"/Audio/Items/pistol_magin.ogg"</example>
        [ViewVariables(VVAccess.ReadWrite)] public string? CellInsertSound = null;

        [ViewVariables] private ContainerSlot _cellContainer = default!;

        [ViewVariables]
        public PowerCellComponent? Cell
        {
            get
            {
                if (_cellContainer.ContainedEntity == null) return null;
                return _cellContainer.ContainedEntity.TryGetComponent(out PowerCellComponent? cell) ? cell : null;
            }
        }

        /// <summary>
        /// True if we don't want a cell inserted during map init.
        /// </summary>
        private bool _startEmpty = false;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref CanRemoveCell, "canRemoveCell", true);
            serializer.DataField(ref _startEmpty, "startEmpty", false);
            serializer.DataField(ref CellRemoveSound, "cellRemoveSound", null);
            serializer.DataField(ref CellInsertSound, "cellInsertSound", null);
        }

        /// <summary>
        /// Remove the cell from this component. If a user is specified, the cell will be put in their hands
        /// or failing that, at their feet. If no user is specified the cell will be put at the location of
        /// the parent of this component.
        /// </summary>
        /// <param name="user">(optional) the user to give the removed cell to.</param>
        /// <param name="playSound">Should <see cref="CellRemoveSound"/> be played upon removal?</param>
        /// <returns>The cell component that was removed, or null if removal failed.</returns>
        public PowerCellComponent? EjectCell(IEntity? user, bool playSound = true)
        {
            var cell = Cell;
            if (cell == null || !CanRemoveCell) return null;
            if (!_cellContainer.Remove(cell.Owner)) return null;
            Dirty();
            if (user != null)
            {
                if (!user.TryGetComponent(out HandsComponent? hands) || !hands.PutInHand(cell.Owner.GetComponent<ItemComponent>()))
                {
                    cell.Owner.Transform.Coordinates = user.Transform.Coordinates;
                }
            }
            else
            {
                cell.Owner.Transform.Coordinates = Owner.Transform.Coordinates;
            }

            if (playSound && CellRemoveSound != null)
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity(CellRemoveSound, Owner);
            }
            return cell;
        }

        public bool InsertCell(IEntity cell, bool playSound = true)
        {

        }

        [Verb]
        public sealed class EjectCellVerb : Verb<PowerCellSlotComponent>
        {
            protected override void GetData(IEntity user, PowerCellSlotComponent component, VerbData data)
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

            protected override void Activate(IEntity user, PowerCellSlotComponent component)
            {
                component.EjectCell(user);
            }
        }

        void IMapInit.MapInit()
        {
            if (_startEmpty || _cellContainer.ContainedEntity != null)
            {
                return;
            }

            string type = SlotSize switch
            {
                PowerCellSize.Small => "PowerCellSmallStandard",
                PowerCellSize.Medium => "PowerCellMediumStandard",
                PowerCellSize.Large => "PowerCellLargeStandard",
                _ => throw new ArgumentOutOfRangeException()
            };

            var cell = Owner.EntityManager.SpawnEntity(type, Owner.Transform.Coordinates);
            _cellContainer.Insert(cell);
        }
    }
}
