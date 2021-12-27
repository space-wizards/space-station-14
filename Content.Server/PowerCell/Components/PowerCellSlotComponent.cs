using System;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.PowerCell.Components
{
    /// <summary>
    /// Provides a "battery compartment" that can contain a <see cref="PowerCellComponent"/> of the matching
    /// <see cref="PowerCellSize"/>. Intended to supplement other components, not very useful by itself.
    /// </summary>
    [RegisterComponent]
#pragma warning disable 618
    public class PowerCellSlotComponent : Component, IExamine, IMapInit
#pragma warning restore 618
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public override string Name => "PowerCellSlot";

        /// <summary>
        /// What size of cell fits into this component.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("slotSize")]
        public PowerCellSize SlotSize { get; set; } = PowerCellSize.Small;

        /// <summary>
        /// Can the cell be removed ?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("canRemoveCell")]
        public bool CanRemoveCell { get; set; } = true;

        /// <summary>
        /// Should the "Remove cell" verb be displayed on this component?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("showVerb")]
        public bool ShowVerb { get; set; } = true;

        /// <summary>
        /// String passed to <see><cref>String.Format</cref></see> when showing the description text for this item.
        /// String.Format is given a single parameter which is the size letter (S/M/L) of the cells this component uses.
        /// Use null to show no text.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("descFormatString")]
        public string? DescFormatString { get; set; } = "It uses size {0} power cells.";

        /// <summary>
        /// File path to a sound file that should be played when the cell is removed.
        /// </summary>
        /// <example>"/Audio/Items/pistol_magout.ogg"</example>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("cellRemoveSound")]
        public SoundSpecifier CellRemoveSound { get; set; } = new SoundPathSpecifier("/Audio/Items/pistol_magin.ogg");

        /// <summary>
        /// File path to a sound file that should be played when a cell is inserted.
        /// </summary>
        /// <example>"/Audio/Items/pistol_magin.ogg"</example>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("cellInsertSound")]
        public SoundSpecifier CellInsertSound { get; set; } = new SoundPathSpecifier("/Audio/Items/pistol_magout.ogg");

        [ViewVariables] private ContainerSlot _cellContainer = default!;

        [ViewVariables]
        public PowerCellComponent? Cell
        {
            get
            {
                if (_cellContainer.ContainedEntity == null) return null;
                return _entities.TryGetComponent(_cellContainer.ContainedEntity.Value, out PowerCellComponent? cell) ? cell : null;
            }
        }

        [ViewVariables] public bool HasCell => Cell != null;

        /// <summary>
        /// True if we don't want a cell inserted during map init.
        /// </summary>
        [DataField("startEmpty")]
        private bool _startEmpty = false;

        /// <summary>
        /// If not null, this cell type will be inserted at MapInit instead of the default Standard cell.
        /// </summary>
        [DataField("startingCellType")]
        private string? _startingCellType = null;

        protected override void Initialize()
        {
            base.Initialize();
            _cellContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, "cellslot_cell_container", out _);
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!inDetailsRange) return;
            string sizeLetter = SlotSize switch
            {
                PowerCellSize.Small => Loc.GetString("power-cell-slot-component-small-size-shorthand"),
                PowerCellSize.Medium => Loc.GetString("power-cell-slot-component-medium-size-shorthand"),
                PowerCellSize.Large => Loc.GetString("power-cell-slot-component-large-size-shorthand"),
                _ => "???"
            };
            if (DescFormatString != null) message.AddMarkup(string.Format(DescFormatString, sizeLetter));
        }

        /// <summary>
        /// Remove the cell from this component. If a user is specified, the cell will be put in their hands
        /// or failing that, at their feet. If no user is specified the cell will be put at the location of
        /// the parent of this component.
        /// </summary>
        /// <param name="user">(optional) the user to give the removed cell to.</param>
        /// <param name="playSound">Should <see cref="CellRemoveSound"/> be played upon removal?</param>
        /// <returns>The cell component of the entity that was removed, or null if removal failed.</returns>
        public PowerCellComponent? EjectCell(EntityUid? user = null, bool playSound = true)
        {
            var cell = Cell;
            if (cell == null || !CanRemoveCell) return null;
            if (!_cellContainer.Remove(cell.Owner)) return null;
            //Dirty();
            if (user != null)
            {
                if (!_entities.TryGetComponent(user, out HandsComponent? hands) || !hands.PutInHand(_entities.GetComponent<ItemComponent>(cell.Owner)))
                {
                    _entities.GetComponent<TransformComponent>(cell.Owner).Coordinates = _entities.GetComponent<TransformComponent>(user.Value).Coordinates;
                }
            }
            else
            {
                _entities.GetComponent<TransformComponent>(cell.Owner).Coordinates = _entities.GetComponent<TransformComponent>(Owner).Coordinates;
            }

            if (playSound)
            {
                SoundSystem.Play(Filter.Pvs(Owner), CellRemoveSound.GetSound(), Owner, AudioHelpers.WithVariation(0.125f));
            }

            _entities.EventBus.RaiseLocalEvent(Owner, new PowerCellChangedEvent(true), false);
            return cell;
        }

        /// <summary>
        /// Tries to insert the given cell into this component. The cell will be put into the container of this component.
        /// </summary>
        /// <param name="cell">The cell to insert.</param>
        /// <param name="playSound">Should <see cref="CellInsertSound"/> be played upon insertion?</param>
        /// <returns>True if insertion succeeded; false otherwise.</returns>
        public bool InsertCell(EntityUid cell, bool playSound = true)
        {
            if (Cell != null) return false;
            if (!_entities.HasComponent<ItemComponent>(cell)) return false;
            if (!_entities.TryGetComponent<PowerCellComponent?>(cell, out var cellComponent)) return false;
            if (cellComponent.CellSize != SlotSize) return false;
            if (!_cellContainer.Insert(cell)) return false;
            //Dirty();
            if (playSound)
            {
                SoundSystem.Play(Filter.Pvs(Owner), CellInsertSound.GetSound(), Owner, AudioHelpers.WithVariation(0.125f));
            }

            _entities.EventBus.RaiseLocalEvent(Owner, new PowerCellChangedEvent(false), false);
            return true;
        }

        void IMapInit.MapInit()
        {
            if (_startEmpty || _cellContainer.ContainedEntity != null)
            {
                return;
            }

            string type;
            if (_startingCellType != null)
            {
                type = _startingCellType;
            }
            else
            {
                type = SlotSize switch
                {
                    PowerCellSize.Small => "PowerCellSmallStandard",
                    PowerCellSize.Medium => "PowerCellMediumStandard",
                    PowerCellSize.Large => "PowerCellLargeStandard",
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            var cell = _entities.SpawnEntity(type, _entities.GetComponent<TransformComponent>(Owner).Coordinates);
            _cellContainer.Insert(cell);
        }
    }

    public class PowerCellChangedEvent : EntityEventArgs
    {
        /// <summary>
        /// If true, the cell was ejected; if false, it was inserted.
        /// </summary>
        public bool Ejected { get; }

        public PowerCellChangedEvent(bool ejected)
        {
            Ejected = ejected;
        }
    }
}
