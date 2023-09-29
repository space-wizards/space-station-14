using Content.Server.Labels.Components;
using Content.Server.Paper;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Labels;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.Labels
{
    /// <summary>
    /// A system that lets players see the contents of a label on an object.
    /// </summary>
    [UsedImplicitly]
    public sealed class LabelSystem : EntitySystem
    {
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public const string ContainerName = "paper_label";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LabelComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<PaperLabelComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<PaperLabelComponent, ComponentRemove>(OnComponentRemove);
            SubscribeLocalEvent<PaperLabelComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
            SubscribeLocalEvent<PaperLabelComponent, EntRemovedFromContainerMessage>(OnContainerModified);
            SubscribeLocalEvent<PaperLabelComponent, ExaminedEvent>(OnExamined);
        }

        /// <summary>
        /// Apply or remove a label on an entity.
        /// </summary>
        /// <param name="uid">EntityUid to change label on</param>
        /// <param name="text">intended label text (null to remove)</param>
        /// <param name="label">label component for resolve</param>
        /// <param name="metadata">metadata component for resolve</param>
        public void Label(EntityUid uid, string? text, MetaDataComponent? metadata = null, LabelComponent? label = null)
        {
            if (!Resolve(uid, ref metadata))
                return;
            if (!Resolve(uid, ref label, false))
                label = EnsureComp<LabelComponent>(uid);

            if (string.IsNullOrEmpty(text))
            {
                if (label.OriginalName is null)
                    return;

                // Remove label
                metadata.EntityName = label.OriginalName;
                label.CurrentLabel = null;
                label.OriginalName = null;

                return;
            }

            // Update label
            label.OriginalName ??= metadata.EntityName;
            label.CurrentLabel = text;
            metadata.EntityName = $"{label.OriginalName} ({text})";
        }

        private void OnComponentInit(EntityUid uid, PaperLabelComponent component, ComponentInit args)
        {
            _itemSlotsSystem.AddItemSlot(uid, ContainerName, component.LabelSlot);

            if (!EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                return;

            _appearance.SetData(uid, PaperLabelVisuals.HasLabel, false, appearance);
        }

        private void OnComponentRemove(EntityUid uid, PaperLabelComponent component, ComponentRemove args)
        {
            _itemSlotsSystem.RemoveItemSlot(uid, component.LabelSlot);
        }

        private void OnExamine(EntityUid uid, LabelComponent? label, ExaminedEvent args)
        {
            if (!Resolve(uid, ref label))
                return;

            if (label.CurrentLabel == null)
                return;

            var message = new FormattedMessage();
            message.AddText(Loc.GetString("hand-labeler-has-label", ("label", label.CurrentLabel)));
            args.PushMessage(message);
        }

        private void OnExamined(EntityUid uid, PaperLabelComponent comp, ExaminedEvent args)
        {
            if (comp.LabelSlot.Item is not {Valid: true} item)
                return;

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
        }

        private void OnContainerModified(EntityUid uid, PaperLabelComponent label, ContainerModifiedMessage args)
        {
            if (!label.Initialized) return;

            if (args.Container.ID != label.LabelSlot.ID)
                return;

            if (!EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                return;

            _appearance.SetData(uid, PaperLabelVisuals.HasLabel, label.LabelSlot.HasItem, appearance);
        }
    }
}
