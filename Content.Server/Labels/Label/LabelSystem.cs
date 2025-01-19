using Content.Server.Labels.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Labels;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Paper;
using FastAccessors;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;
using System.Linq;
using Content.Shared.IdentityManagement;
using Robust.Shared.GameObjects;

namespace Content.Server.Labels
{
    /// <summary>
    /// A system that lets players see the contents of a label on an object.
    /// </summary>
    [UsedImplicitly]
    public sealed class LabelSystem : SharedLabelSystem
    {
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public const string ContainerName = "paper_label";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PaperLabelComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<PaperLabelComponent, ComponentRemove>(OnComponentRemove);
            SubscribeLocalEvent<PaperLabelComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
            SubscribeLocalEvent<PaperLabelComponent, EntRemovedFromContainerMessage>(OnContainerModified);
            // Harmony - OnExamined can now be inventory relayed from the neck slot for lanyard implementation.
            // OnExamined is called with a true bool when the event is from an inventory relayed event,
            // and a false bool when it is not. This changes the label inspection text to reflect it being from a lanyard.
            // This would cause any other neck slot item with the label component to be described as a lanyard on inspection,
            // but currently no others exist. 
            SubscribeLocalEvent<PaperLabelComponent, ExaminedEvent>((e, c, ev) => OnExamined(e, c, ev, false));
            SubscribeLocalEvent<PaperLabelComponent, InventoryRelayedEvent<ExaminedEvent>>((e, c, ev) => OnExamined(e, c, ev.Args, true));
        }

        /// <summary>
        /// Apply or remove a label on an entity.
        /// </summary>
        /// <param name="uid">EntityUid to change label on</param>
        /// <param name="text">intended label text (null to remove)</param>
        /// <param name="label">label component for resolve</param>
        /// <param name="metadata">metadata component for resolve</param>
        public override void Label(EntityUid uid, string? text, MetaDataComponent? metadata = null, LabelComponent? label = null)
        {
            if (!Resolve(uid, ref label, false))
                label = EnsureComp<LabelComponent>(uid);

            label.CurrentLabel = text;
            NameMod.RefreshNameModifiers(uid);

            Dirty(uid, label);
        }

        private void OnComponentInit(EntityUid uid, PaperLabelComponent component, ComponentInit args)
        {
            _itemSlotsSystem.AddItemSlot(uid, ContainerName, component.LabelSlot);

            UpdateAppearance((uid, component));
        }

        private void OnComponentRemove(EntityUid uid, PaperLabelComponent component, ComponentRemove args)
        {
            _itemSlotsSystem.RemoveItemSlot(uid, component.LabelSlot);
        }

        private void OnExamined(EntityUid uid, PaperLabelComponent comp, ExaminedEvent args, bool lanyard)
        {
            if (comp.LabelSlot.Item is not { Valid: true } item)
            {
                return;
            }
            using (args.PushGroup(nameof(PaperLabelComponent)))
            {
                // Harmony - uid's parent is saved to be used for localisation grammar if the label is from a lanyard.
                // If it is not from a lanyard, then this does nothing.
                var user = EntityManager.GetComponent<TransformComponent>(uid).ParentUid;
                if (!args.IsInDetailsRange)
                {
                    // Harmony - if this label component was relayed from the inventory, text is changed to"[They are] wearing a lanyard".
                    if (lanyard == true)
                        args.PushMarkup(Loc.GetString("comp-lanyard-has-lanyard-cant-read", ("user", Identity.Entity(user, EntityManager))));
                    else
                        args.PushMarkup(Loc.GetString("comp-paper-label-has-label-cant-read"));
                    return;
                }

                if (!EntityManager.TryGetComponent(item, out PaperComponent? paper))
                    // Assuming yaml has the correct entity whitelist, this should not happen.
                    return;

                if (string.IsNullOrWhiteSpace(paper.Content))
                {
                    // Harmony - same as above.
                    if (lanyard == true)
                        args.PushMarkup(Loc.GetString("comp-lanyard-has-lanyard-blank", ("user", Identity.Entity(user, EntityManager))));
                    else
                        args.PushMarkup(Loc.GetString("comp-paper-label-has-label-blank"));
                    return;
                }
                // Harmony - same as above.
                if (lanyard == true)
                    args.PushMarkup(Loc.GetString("comp-lanyard-has-lanyard", ("user", Identity.Entity(user, EntityManager))));
                else
                    args.PushMarkup(Loc.GetString("comp-paper-label-has-label"));
                var text = paper.Content;
                args.PushMarkup(text.TrimEnd());
                // Harmony - shows which stamps have been applied to a label when inspected. Copied from PaperSystem.
                if (paper.StampedBy.Count > 0)
                {
                    var commaSeparated =
                        string.Join(", ", paper.StampedBy.Select(s => Loc.GetString(s.StampedName)));
                    args.PushMarkup(
                        Loc.GetString(
                            "comp-label-examine-detail-stamped-by",
                            ("stamps", commaSeparated))
                    );
                }
            }
        }

        private void OnContainerModified(EntityUid uid, PaperLabelComponent label, ContainerModifiedMessage args)
        {
            if (!label.Initialized) return;

            if (args.Container.ID != label.LabelSlot.ID)
                return;

            UpdateAppearance((uid, label));
        }

        private void UpdateAppearance(Entity<PaperLabelComponent, AppearanceComponent?> ent)
        {
            if (!Resolve(ent, ref ent.Comp2, false))
                return;

            var slot = ent.Comp1.LabelSlot;
            _appearance.SetData(ent, PaperLabelVisuals.HasLabel, slot.HasItem, ent.Comp2);
            if (TryComp<PaperLabelTypeComponent>(slot.Item, out var type))
                _appearance.SetData(ent, PaperLabelVisuals.LabelType, type.PaperType, ent.Comp2);
        }
    }
}
