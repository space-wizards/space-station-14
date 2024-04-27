using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// Provides API for openable food and drinks, handles opening on use and preventing transfer when closed.
/// </summary>
public sealed partial class OpenableSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OpenableComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<OpenableComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<OpenableComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<OpenableComponent, MeleeHitEvent>(HandleIfClosed);
        SubscribeLocalEvent<OpenableComponent, AfterInteractEvent>(HandleIfClosed);
        SubscribeLocalEvent<OpenableComponent, GetVerbsEvent<Verb>>(AddOpenCloseVerbs);
        SubscribeLocalEvent<OpenableComponent, SolutionTransferAttemptEvent>(OnTransferAttempt);
        SubscribeLocalEvent<OpenableComponent, AttemptShakeEvent>(OnAttemptShake);
        SubscribeLocalEvent<OpenableComponent, AttemptAddFizzinessEvent>(OnAttemptAddFizziness);
    }

    private void OnInit(EntityUid uid, OpenableComponent comp, ComponentInit args)
    {
        UpdateAppearance(uid, comp);
    }

    private void OnUse(EntityUid uid, OpenableComponent comp, UseInHandEvent args)
    {
        if (args.Handled || !comp.OpenableByHand)
            return;

        args.Handled = TryOpen(uid, comp, args.User);
    }

    private void OnExamined(EntityUid uid, OpenableComponent comp, ExaminedEvent args)
    {
        if (!comp.Opened || !args.IsInDetailsRange)
            return;

        var text = Loc.GetString(comp.ExamineText);
        args.PushMarkup(text);
    }

    private void HandleIfClosed(EntityUid uid, OpenableComponent comp, HandledEntityEventArgs args)
    {
        // prevent spilling/pouring/whatever drinks when closed
        args.Handled = !comp.Opened;
    }

    private void AddOpenCloseVerbs(EntityUid uid, OpenableComponent comp, GetVerbsEvent<Verb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        Verb verb;
        if (comp.Opened)
        {
            if (!comp.Closeable)
                return;

            verb = new()
            {
                Text = Loc.GetString(comp.CloseVerbText),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/close.svg.192dpi.png")),
                Act = () => TryClose(args.Target, comp, args.User)
            };
        }
        else
        {
            verb = new()
            {
                Text = Loc.GetString(comp.OpenVerbText),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/open.svg.192dpi.png")),
                Act = () => TryOpen(args.Target, comp, args.User)
            };
        }
        args.Verbs.Add(verb);
    }

    private void OnTransferAttempt(Entity<OpenableComponent> ent, ref SolutionTransferAttemptEvent args)
    {
        if (!ent.Comp.Opened)
        {
            // message says its just for drinks, shouldn't matter since you typically dont have a food that is openable and can be poured out
            args.Cancel(Loc.GetString("drink-component-try-use-drink-not-open", ("owner", ent.Owner)));
        }
    }

    private void OnAttemptShake(Entity<OpenableComponent> entity, ref AttemptShakeEvent args)
    {
        // Prevent shaking open containers
        if (entity.Comp.Opened)
            args.Cancelled = true;
    }

    private void OnAttemptAddFizziness(Entity<OpenableComponent> entity, ref AttemptAddFizzinessEvent args)
    {
        // Can't add fizziness to an open container
        if (entity.Comp.Opened)
            args.Cancelled = true;
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
    public void SetOpen(EntityUid uid, bool opened = true, OpenableComponent? comp = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref comp, false) || opened == comp.Opened)
            return;

        comp.Opened = opened;
        Dirty(uid, comp);

        if (opened)
        {
            var ev = new OpenableOpenedEvent(user);
            RaiseLocalEvent(uid, ref ev);
        }
        else
        {
            var ev = new OpenableClosedEvent(user);
            RaiseLocalEvent(uid, ref ev);
        }

        UpdateAppearance(uid, comp);
    }

    /// <summary>
    /// If closed, opens it and plays the sound.
    /// </summary>
    /// <returns>Whether it got opened</returns>
    public bool TryOpen(EntityUid uid, OpenableComponent? comp = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref comp, false) || comp.Opened)
            return false;

        SetOpen(uid, true, comp, user);
        _audio.PlayPredicted(comp.Sound, uid, user);
        return true;
    }

    /// <summary>
    /// If opened, closes it and plays the close sound, if one is defined.
    /// </summary>
    /// <returns>Whether it got closed</returns>
    public bool TryClose(EntityUid uid, OpenableComponent? comp = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref comp, false) || !comp.Opened || !comp.Closeable)
            return false;

        SetOpen(uid, false, comp, user);
        if (comp.CloseSound != null)
            _audio.PlayPredicted(comp.CloseSound, uid, user);
        return true;
    }
}

/// <summary>
/// Raised after an Openable is opened.
/// </summary>
[ByRefEvent]
public record struct OpenableOpenedEvent(EntityUid? User = null);

/// <summary>
/// Raised after an Openable is closed.
/// </summary>
[ByRefEvent]
public record struct OpenableClosedEvent(EntityUid? User = null);
