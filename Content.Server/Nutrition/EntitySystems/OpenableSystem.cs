using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;

namespace Content.Server.Nutrition.EntitySystems;

/// <summary>
/// Provides API for openable food and drinks, handles opening on use and preventing transfer when closed.
/// </summary>
public sealed class OpenableSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OpenableComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<OpenableComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<OpenableComponent, ExaminedEvent>(OnExamined, after: new[] { typeof(PuddleSystem) });
        SubscribeLocalEvent<OpenableComponent, SolutionTransferAttemptEvent>(OnTransferAttempt);
        SubscribeLocalEvent<OpenableComponent, MeleeHitEvent>(HandleIfClosed);
        SubscribeLocalEvent<OpenableComponent, AfterInteractEvent>(HandleIfClosed);
    }

    private void OnInit(EntityUid uid, OpenableComponent comp, ComponentInit args)
    {
        UpdateAppearance(uid, comp);
    }

    private void OnUse(EntityUid uid, OpenableComponent comp, UseInHandEvent args)
    {
        if (args.Handled || !comp.OpenableByHand)
            return;

        args.Handled = TryOpen(uid, comp);
    }

    private void OnExamined(EntityUid uid, OpenableComponent comp, ExaminedEvent args)
    {
        if (!comp.Opened || !args.IsInDetailsRange)
            return;

        var text = Loc.GetString(comp.ExamineText);
        args.PushMarkup(text);
    }

    private void OnTransferAttempt(EntityUid uid, OpenableComponent comp, SolutionTransferAttemptEvent args)
    {
        if (!comp.Opened)
        {
            // message says its just for drinks, shouldn't matter since you typically dont have a food that is openable and can be poured out
            args.Cancel(Loc.GetString("drink-component-try-use-drink-not-open", ("owner", uid)));
        }
    }

    private void HandleIfClosed(EntityUid uid, OpenableComponent comp, HandledEntityEventArgs args)
    {
        // prevent spilling/pouring/whatever drinks when closed
        args.Handled = !comp.Opened;
    }

    /// <summary>
    /// Returns true if the entity either does not have OpenableComponent or it is opened.
    /// Drinks that don't have OpenableComponent are automatically open, so it returns true.
    /// </summary>
    public bool IsOpen(EntityUid uid, OpenableComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return true;

        return comp.Opened;
    }

    /// <summary>
    /// Returns true if the entity both has OpenableComponent and is not opened.
    /// Drinks that don't have OpenableComponent are automatically open, so it returns false.
    /// If user is not null a popup will be shown to them.
    /// </summary>
    public bool IsClosed(EntityUid uid, EntityUid? user = null, OpenableComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return false;

        if (comp.Opened)
            return false;

        if (user != null)
            _popup.PopupEntity(Loc.GetString(comp.ClosedPopup, ("owner", uid)), user.Value, user.Value);

        return true;
    }

    /// <summary>
    /// Update open visuals to the current value.
    /// </summary>
    public void UpdateAppearance(EntityUid uid, OpenableComponent? comp = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        _appearance.SetData(uid, OpenableVisuals.Opened, comp.Opened, appearance);
    }

    /// <summary>
    /// Sets the opened field and updates open visuals.
    /// </summary>
    public void SetOpen(EntityUid uid, bool opened = true, OpenableComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) || opened == comp.Opened)
            return;

        comp.Opened = opened;

        UpdateAppearance(uid, comp);
    }

    /// <summary>
    /// If closed, opens it and plays the sound.
    /// </summary>
    /// <returns>Whether it got opened</returns>
    public bool TryOpen(EntityUid uid, OpenableComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) || comp.Opened)
            return false;

        SetOpen(uid, true, comp);
        _audio.PlayPvs(comp.Sound, uid);
        return true;
    }
}
