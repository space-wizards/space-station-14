using Content.Server.Labels.Components;
using Content.Server.Paper;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Labels;
using Content.Shared.Labels.Components;
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
<<<<<<< HEAD
        [Dependency] private readonly MetaDataSystem _metadata = default!;
=======
        [Dependency] private readonly MetaDataSystem _metaData = default!;
>>>>>>> a5773e70db935ff5d584d68055607e1de462f49d

        public const string ContainerName = "paper_label";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LabelComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<LabelComponent, ComponentStartup>(OnStartup);
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
                _metadata.SetEntityName(uid, label.OriginalName, metadata);
                label.CurrentLabel = null;

                return;
            }

            // Update label
            label.OriginalName ??= metadata.EntityName;
            label.CurrentLabel = text;
            _metadata.SetEntityName(uid, $"{label.OriginalName} ({text})", metadata);
        }

        public void UpdateLabel(EntityUid uid, LabelComponent? label = null)
        {
            if (!Resolve(uid, ref label))
                return;

            _metadata.SetEntityName(uid, $"{label.OriginalName} ({label.CurrentLabel})");
        }

        private void OnStartup(EntityUid uid, LabelComponent component, ComponentStartup args)
        {
            if(string.IsNullOrEmpty(component.OriginalName))
                component.OriginalName = Name(uid);

            if (component.CurrentLabel != null)
                UpdateLabel(uid, component);
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
