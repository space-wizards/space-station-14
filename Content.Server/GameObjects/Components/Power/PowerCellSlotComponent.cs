#nullable enable
using System;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.Audio;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Provides a "battery compartment" that can contain a <see cref="PowerCellComponent"/> of the matching
    /// <see cref="PowerCellSize"/>. Intended to supplement other components, not very useful by itself.
    /// </summary>
    [RegisterComponent]
    public class PowerCellSlotComponent : Component, IExamine, IMapInit
    {
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
        public string? CellRemoveSound { get; set; } = "/Audio/Items/pistol_magin.ogg";

        /// <summary>
        /// File path to a sound file that should be played when a cell is inserted.
        /// </summary>
        /// <example>"/Audio/Items/pistol_magin.ogg"</example>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("cellInsertSound")]
        public string? CellInsertSound { get; set; } = "/Audio/Items/pistol_magout.ogg";

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

        public override void Initialize()
        {
            base.Initialize();
            _cellContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, "cellslot_cell_container", out _);
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!inDetailsRange) return;
            string sizeLetter = SlotSize switch
            {
                PowerCellSize.Small => Loc.GetString("S"),
                PowerCellSize.Medium => Loc.GetString("M"),
                PowerCellSize.Large => Loc.GetString("L"),
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
        public PowerCellComponent? EjectCell(IEntity? user, bool playSound = true)
        {
            var cell = Cell;
            if (cell == null || !CanRemoveCell) return null;
            if (!_cellContainer.Remove(cell.Owner)) return null;
            //Dirty();
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
                EntitySystem.Get<AudioSystem>().PlayFromEntity(CellRemoveSound, Owner, AudioHelpers.WithVariation(0.125f));
            }
            SendMessage(new PowerCellChangedMessage(true));
            return cell;
        }

        /// <summary>
        /// Tries to insert the given cell into this component. The cell will be put into the container of this component.
        /// </summary>
        /// <param name="cell">The cell to insert.</param>
        /// <param name="playSound">Should <see cref="CellInsertSound"/> be played upon insertion?</param>
        /// <returns>True if insertion succeeded; false otherwise.</returns>
        public bool InsertCell(IEntity cell, bool playSound = true)
        {
            if (Cell != null) return false;
            if (!cell.TryGetComponent<ItemComponent>(out var _)) return false;
            if (!cell.TryGetComponent<PowerCellComponent>(out var cellComponent)) return false;
            if (cellComponent.CellSize != SlotSize) return false;
            if (!_cellContainer.Insert(cell)) return false;
            //Dirty();
            if (playSound && CellInsertSound != null)
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity(CellInsertSound, Owner, AudioHelpers.WithVariation(0.125f));
            }
            SendMessage(new PowerCellChangedMessage(false));
            return true;
        }

        [Verb]
        public sealed class EjectCellVerb : Verb<PowerCellSlotComponent>
        {
            protected override void GetData(IEntity user, PowerCellSlotComponent component, VerbData data)
            {
                if (!component.ShowVerb || !ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (component.Cell == null)
                {
                    data.Text = Loc.GetString("No cell");
                    data.Visibility = VerbVisibility.Disabled;
                }
                else
                {
                    data.Text = Loc.GetString("Eject cell");
                    data.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.96dpi.png";
                }

                if (component.Cell == null || !component.CanRemoveCell)
                {
                    data.Visibility = VerbVisibility.Disabled;
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

            var cell = Owner.EntityManager.SpawnEntity(type, Owner.Transform.Coordinates);
            _cellContainer.Insert(cell);
        }
    }

    public class PowerCellChangedMessage : ComponentMessage
    {
        /// <summary>
        /// If true, the cell was ejected; if false, it was inserted.
        /// </summary>
        public bool Ejected { get; }

        public PowerCellChangedMessage(bool ejected)
        {
            Ejected = ejected;
        }
    }
}
