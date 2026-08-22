using System.Diagnostics.CodeAnalysis;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Labels.Components;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.Photography;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared.Labels.EntitySystems;

/// <summary>
/// A system for applying labels to entities.
/// Currently handles logic for the hand labeler and paper labels.
/// </summary>
/// <seealso cref="LabelComponent"/>
/// <seealso cref="PaperLabelComponent"/>
public sealed partial class LabelSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private NameModifierSystem _nameModifier = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [Dependency] private EntityQuery<PaperLabelTypeComponent> _labelTypeQuery = default!;

    public const string ContainerName = "paper_label";

    #region Event Handlers
    [SubscribeLocalEvent]
    private void OnLabelCompMapInit(Entity<LabelComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.LocalizedLabel is { } locId)
        {
            ent.Comp.CurrentLabel = Loc.GetString(locId);
            Dirty(ent);
        }

        _nameModifier.RefreshNameModifiers(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnLabelShutdown(Entity<LabelComponent> ent, ref ComponentShutdown args)
    {
        _nameModifier.RefreshNameModifiers(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnExamine(Entity<LabelComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Examinable)
            return;

        if (ent.Comp.CurrentLabel == null)
            return;

        var message = new FormattedMessage();
        message.AddText(Loc.GetString("hand-labeler-has-label", ("label", ent.Comp.CurrentLabel)));
        args.PushMessage(message);
    }

    [SubscribeLocalEvent]
    private void OnRefreshNameModifiers(Entity<LabelComponent> entity, ref RefreshNameModifiersEvent args)
    {
        // We need to check lifestage so labels queued for deferred removal don't get applied.
        if (!string.IsNullOrEmpty(entity.Comp.CurrentLabel) && entity.Comp.LifeStage < ComponentLifeStage.Stopping)
            args.AddModifier("comp-label-format", extraArgs: ("label", entity.Comp.CurrentLabel));
    }

    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<PaperLabelComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent.Owner, ContainerName, ent.Comp.LabelSlot);

        UpdateAppearance(ent);
    }

    [SubscribeLocalEvent]
    private void OnComponentRemove(Entity<PaperLabelComponent> ent, ref ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(ent.Owner, ent.Comp.LabelSlot);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<PaperLabelComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.LabelSlot.Item is not { Valid: true } item)
            return;

        using (args.PushGroup(nameof(PaperLabelComponent)))
        {
            if (!args.IsInDetailsRange)
            {
                args.PushMarkup(Loc.GetString("comp-paper-label-has-label-cant-read"));
                return;
            }

            LabelExaminedEvent ev = new(args);
            RaiseLocalEvent(item, ref ev);
        }
    }

    [SubscribeLocalEvent]
    private void OnInsertedIntoContainer(Entity<PaperLabelComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        HandleModified(ent, args);
    }

    [SubscribeLocalEvent]
    private void OnRemovedFromContainer(Entity<PaperLabelComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        HandleModified(ent, args);
    }

    [SubscribeLocalEvent]
    private void OnPaperLabelExamined(Entity<PaperComponent> ent, ref LabelExaminedEvent args)
    {
        if (args.Handled)
            return;

        if (string.IsNullOrWhiteSpace(ent.Comp.Content))
        {
            args.Examined.PushMarkup(Loc.GetString("comp-paper-label-has-label-blank"));
            return;
        }

        args.Examined.PushMarkup(Loc.GetString("comp-paper-label-has-label"));
        var text = ent.Comp.Content;
        args.Examined.PushMarkup(text.TrimEnd());
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnPhotographLabelExamined(Entity<PhotographComponent> ent, ref LabelExaminedEvent args)
    {
        if (args.Handled)
            return;

        string nameText = string.IsNullOrEmpty(ent.Comp.NameText)
            ? Loc.GetString("photograph-name-text-empty")
            : ent.Comp.NameText;
        args.Examined.PushText(Loc.GetString("photograph-label-examine", ("text", nameText)));

        if (ent.Comp.Description != null)
            args.Examined.PushMessage(new FormattedMessage(ent.Comp.Description));

        args.Handled = true;
    }
    #endregion Event Handlers

    #region Public API
    /// <summary>
    /// Add, change, or remove a label on an entity.
    /// </summary>
    /// <remarks>
    /// If <paramref name="text"/> is <see langword="null"/> or an empty string, the <see cref="LabelComponent"/> will be removed.
    /// </remarks>
    /// <param name="uid">EntityUid to change label on</param>
    /// <param name="text">intended label text (null to remove)</param>
    /// <param name="label">label component for resolve</param>
    /// <param name="metadata">metadata component for resolve</param>
    // TODO - Change signature to `Label(Entity<LabelComponent?> ent, string? text)`
    [PublicAPI]
    public void Label(EntityUid uid, string? text, MetaDataComponent? metadata = null, LabelComponent? label = null)
    {
        // If setting the label to be blank, just remove the label.
        if (string.IsNullOrEmpty(text))
        {
            RemoveLabel((uid, label));
            return;
        }

        label = EnsureComp<LabelComponent>(uid);

        label.CurrentLabel = FormattedMessage.EscapeText(text);
        _nameModifier.RefreshNameModifiers(uid);

        Dirty(uid, label);
    }

    /// <summary>
    /// Removes the label from an entity.
    /// </summary>
    /// <param name="ent">The entity from which the label should be removed.</param>
    /// <returns>true if a label was removed, or false if the entity already didn't have a label.</returns>
    [PublicAPI]
    public bool RemoveLabel(Entity<LabelComponent?> ent)
    {
        return RemComp<LabelComponent>(ent);
    }

    /// <summary>
    /// Returns the text of the label from an entity, or <see langword="null"/> if it doesn't have a label.
    /// </summary>
    /// <param name="ent">The entity from which to get the label text.</param>
    [Pure]
    [PublicAPI]
    public string? GetLabelText(Entity<LabelComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false))
            return null;

        return ent.Comp.CurrentLabel;
    }

    /// <summary>
    /// Returns true if an entity has a visible label.
    /// </summary>
    /// <param name="ent">The entity to check for a label.</param>
    [Pure]
    [PublicAPI]
    public bool HasLabel(EntityUid ent)
    {
        return HasComp<LabelComponent>(ent);
    }

    /// <summary>
    /// Retrieves a label with the specified component from the default label slot.
    /// </summary>
    [PublicAPI]
    public bool TryGetLabel<T>(Entity<PaperLabelComponent?> ent, [NotNullWhen(true)] out Entity<T>? label) where T : Component
    {
        label = null;
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.LabelSlot.Item is not { } labelEnt)
            return false;

        if (!TryComp<T>(labelEnt, out var labelComp))
            return false;

        label = (labelEnt, labelComp);
        return true;
    }
    #endregion Public API

    #region Internal
    private void HandleModified(Entity<PaperLabelComponent> ent, ContainerModifiedMessage args)
    {
        if (!ent.Comp.Initialized)
            return;

        if (args.Container.ID != ent.Comp.LabelSlot.ID)
            return;

        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<PaperLabelComponent, AppearanceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return;

        var labelType = PaperLabelType.None;
        if (_labelTypeQuery.TryComp(ent.Comp1.LabelSlot.Item, out var type))
        {
            labelType = type.LabelType;
            _appearance.SetData(ent, PaperLabelVisuals.LabelColor, type.Color, ent.Comp2);
        }
        _appearance.SetData(ent, PaperLabelVisuals.LabelType, labelType, ent.Comp2);
    }
    #endregion Internal
}
