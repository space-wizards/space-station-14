using Content.Server.Labels.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Labels;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Paper;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using System.Linq;
using Content.Shared.IdentityManagement;

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
            SubscribeLocalEvent<PaperLabelComponent, ExaminedEvent>(OnExamined);
            // Harmony - OnExamined can now be inventory relayed from the neck slot for lanyard implementation,
            // getting the entity's pronouns and changing the label inspection text to reflect it being from a lanyard.
            // This would cause any other neck slot item with the label component to be described as a lanyard on inspection,
            // but currently no others exist. 
            SubscribeLocalEvent<PaperLabelComponent, InventoryRelayedEvent<ExaminedEvent>>((e, c, ev) => OnExaminedInInventory(e, c, ev.Args));
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

        private void OnExamined(EntityUid uid, PaperLabelComponent comp, ExaminedEvent args)
        {
            if (comp.LabelSlot.Item is not { Valid: true } item)
                return;

            using (args.PushGroup(nameof(PaperLabelComponent)))
            {
                if (!args.IsInDetailsRange)
                {
                    args.PushMarkup(Loc.GetString("comp-paper-label-has-label-cant-read"));
                    return;
                }

                if (!EntityManager.TryGetComponent(item, out PaperComponent? paper))
                    // Assuming yaml has the correct entity whitelist, this should not happen.
                    return;

                if (string.IsNullOrWhiteSpace(paper.Content))
                {
                    args.PushMarkup(Loc.GetString("comp-paper-label-has-label-blank"));
                    return;
                }

                args.PushMarkup(Loc.GetString("comp-paper-label-has-label"));
                var text = paper.Content;
                args.PushMarkup(text.TrimEnd());
                // Harmony addition begins - shows which stamps have been applied to a label when inspected. Copied from PaperSystem.
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
                // Harmony addition ends
            }
        }

        // Harmony addition begins - version of OnExamined for when the event is inventory relayed. Used when reading from a worn lanyard.
        private void OnExaminedInInventory(EntityUid uid, PaperLabelComponent comp, ExaminedEvent args)
        {
            if (comp.LabelSlot.Item is not { Valid: true } item)
                return;

            using (args.PushGroup(nameof(PaperLabelComponent)))
            {
                // UID's parent is saved to be used for localisation grammar when the label is from a lanyard, since the text is changed.
                var user = EntityManager.GetComponent<TransformComponent>(uid).ParentUid;
                if (!args.IsInDetailsRange)
                {
                    args.PushMarkup(Loc.GetString("comp-lanyard-has-lanyard-cant-read", ("user", Identity.Entity(user, EntityManager))));
                    return;
                }

                if (!EntityManager.TryGetComponent(item, out PaperComponent? paper))
                    // Assuming yaml has the correct entity whitelist, this should not happen.
                    return;

                if (string.IsNullOrWhiteSpace(paper.Content))
                {
                    args.PushMarkup(Loc.GetString("comp-lanyard-has-lanyard-blank", ("user", Identity.Entity(user, EntityManager))));
                    return;
                }

                args.PushMarkup(Loc.GetString("comp-lanyard-has-lanyard", ("user", Identity.Entity(user, EntityManager))));
                var text = paper.Content;
                args.PushMarkup(text.TrimEnd());
                // Harmony - shows which stamps have been applied to a lanyard's label when inspected. Copied from PaperSystem.
                if (paper.StampedBy.Count > 0)
                {
                    var commaSeparated =
                        string.Join(", ", paper.StampedBy.Select(s => Loc.GetString(s.StampedName)));
                    args.PushMarkup(
                        Loc.GetString(
                            "comp-lanyard-examine-detail-stamped-by",
                            ("stamps", commaSeparated))
                    );
                }
            }
        }
        // Harmony addition ends

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
