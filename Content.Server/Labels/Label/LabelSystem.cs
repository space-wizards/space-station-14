using Content.Server.Labels.Components;
using Content.Server.Paper;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Labels;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using System;

namespace Content.Server.Labels
{
    /// <summary>
    /// A system that lets players see the contents of a label on an object.
    /// </summary>
    [UsedImplicitly]
    public class LabelSystem : EntitySystem
    {
        [Dependency] private readonly SharedItemSlotsSystem _itemSlotsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LabelComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<PaperLabelComponent, ComponentInit>(InitializePaperLabel);
            SubscribeLocalEvent<PaperLabelComponent, ItemSlotChangedEvent>(OnItemSlotChanged);
            SubscribeLocalEvent<PaperLabelComponent, ExaminedEvent>(OnExamined);
        }

        private void InitializePaperLabel(EntityUid uid, PaperLabelComponent component, ComponentInit args)
        {
            if (!EntityManager.TryGetComponent(uid, out SharedAppearanceComponent appearance))
                return;

            appearance.SetData(PaperLabelVisuals.HasLabel, false);
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
            if (!EntityManager.TryGetComponent(uid, out SharedItemSlotsComponent slots))
                return;

            var label = _itemSlotsSystem.PeekItemInSlot(slots, comp.LabelSlot);

            if (label == null)
                return;

            if (!args.IsInDetailsRange)
            {
                args.PushMarkup(Loc.GetString("comp-paper-label-has-label-cant-read"));
                return;
            }

            if (!EntityManager.TryGetComponent(label.Uid, out PaperComponent paper))
                // should never happen
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


        private void OnItemSlotChanged(EntityUid uid, PaperLabelComponent component, ItemSlotChangedEvent args)
        {
            if (args.SlotName != component.LabelSlot)
                return;

            if (!EntityManager.TryGetComponent(uid, out SharedAppearanceComponent appearance))
                return;

            appearance.SetData(PaperLabelVisuals.HasLabel, args.ContainedItem != null);
        }
    }
}
