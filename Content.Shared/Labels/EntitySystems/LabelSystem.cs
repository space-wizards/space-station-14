using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Labels.Components;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Paper;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared.Labels.EntitySystems;

public sealed class LabelSystem : EntitySystem
{
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public const string ContainerName = "paper_label";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LabelComponent, MapInitEvent>(OnLabelCompMapInit);
        SubscribeLocalEvent<LabelComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<LabelComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);

        SubscribeLocalEvent<PaperLabelComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PaperLabelComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<PaperLabelComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<PaperLabelComponent, EntRemovedFromContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<PaperLabelComponent, ExaminedEvent>(OnExamined);
    }

    private void OnLabelCompMapInit(EntityUid uid, LabelComponent component, MapInitEvent args)
    {
        if (!string.IsNullOrEmpty(component.CurrentLabel))
        {
            component.CurrentLabel = Loc.GetString(component.CurrentLabel);
            Dirty(uid, component);
        }

        _nameModifier.RefreshNameModifiers(uid);
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
        if (!Resolve(uid, ref label, false))
            label = EnsureComp<LabelComponent>(uid);

        label.CurrentLabel = text;
        _nameModifier.RefreshNameModifiers(uid);

        Dirty(uid, label);
    }

    private void OnExamine(EntityUid uid, LabelComponent? label, ExaminedEvent args)
    {
        if (!Resolve(uid, ref label))
            return;

        if (!label.Examinable)
            return;

        if (label.CurrentLabel == null)
            return;

        var message = new FormattedMessage();
        message.AddText(Loc.GetString("hand-labeler-has-label", ("label", label.CurrentLabel)));
        args.PushMessage(message);
    }

    private void OnRefreshNameModifiers(Entity<LabelComponent> entity, ref RefreshNameModifiersEvent args)
    {
        if (!string.IsNullOrEmpty(entity.Comp.CurrentLabel))
            args.AddModifier("comp-label-format", extraArgs: ("label", entity.Comp.CurrentLabel));
    }

    private void OnComponentInit(EntityUid uid, PaperLabelComponent component, ComponentInit args)
    {
        _itemSlots.AddItemSlot(uid, ContainerName, component.LabelSlot);

        UpdateAppearance((uid, component));
    }

    private void OnComponentRemove(EntityUid uid, PaperLabelComponent component, ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(uid, component.LabelSlot);
    }

    private void OnExamined(EntityUid uid, PaperLabelComponent comp, ExaminedEvent args)
    {
        if (comp.LabelSlot.Item is not {Valid: true} item)
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
        }
    }

    private void OnContainerModified(EntityUid uid, PaperLabelComponent label, ContainerModifiedMessage args)
    {
        if (!label.Initialized)
            return;

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
