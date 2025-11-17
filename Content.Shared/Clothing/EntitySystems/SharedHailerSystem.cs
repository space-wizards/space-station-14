using Content.Shared.Access.Systems;
using Content.Shared.Chat;
using Content.Shared.Clothing.ActionEvent;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.Event;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stealth.Components;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing.EntitySystems;

public abstract class SharedHailerSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HailerComponent, ClothingGotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<HailerComponent, ClothingGotUnequippedEvent>(OnUnequip);
        SubscribeLocalEvent<HailerComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HailerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<HailerComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<HailerComponent, HailerActionEvent>(OnHailAction);
        SubscribeLocalEvent<HailerComponent, HailerOrderMessage>(OnHailOrder);
        SubscribeLocalEvent<HailerComponent, HailerToolDoAfterEvent>(OnToolDoAfter);
        SubscribeLocalEvent<HailerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<HailerComponent, ItemMaskToggledEvent>(OnMaskToggle);
    }

    private void OnEquip(Entity<HailerComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ent.Comp.User = args.Wearer;
    }
    private void OnUnequip(Entity<HailerComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        ent.Comp.User = null;
    }

    private void OnMaskToggle(Entity<HailerComponent> ent, ref ItemMaskToggledEvent args)
    {
        if (TryComp<MaskComponent>(ent, out var mask) && mask.IsToggled)
            _ui.CloseUi(ent.Owner, HailerUiKey.Key);
    }

    private void OnExamine(Entity<HailerComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<EmaggedComponent>(ent))
            args.PushMarkup(Loc.GetString("hailer-gas-mask-emag"));
        else if (ent.Comp.AreWiresCut)
            args.PushMarkup(Loc.GetString("hailer-gas-mask-wires-cut"));
        else
        {
            string desc;
            if (ent.Comp.CurrentHailLevel.HasValue)
                desc = Loc.GetString(ent.Comp.DescriptionLocale, ("level", ent.Comp.CurrentHailLevel.Value.Name)); //Loc string showing aggression level
            else
                desc = Loc.GetString(ent.Comp.DescriptionLocale);
            args.PushMarkup(desc);
        }
    }

    private void OnEmagged(Entity<HailerComponent> ent, ref GotEmaggedEvent args)
    {
        if (args.Handled || HasComp<EmaggedComponent>(ent) || ent.Comp.EmagLevelPrefix == null)
            return;

        if (ent.Comp.User.HasValue && ent.Comp.User != args.UserUid)
            return;

        _popup.PopupPredicted(Loc.GetString("hailer-gas-mask-emagged"), Loc.GetString("hailer-gas-mask-emagged"), ent.Owner, args.UserUid);

        args.Type = EmagType.Interaction;

        args.Handled = true;
        Dirty(ent);
    }

    private void OnGetVerbs(Entity<HailerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        //Cooldown to prevent spamming
        if (_gameTiming.CurTime < ent.Comp.TimeVerbReady)
            return;

        if (!args.CanAccess || !args.CanInteract || ent.Comp.User != args.User)
            return;

        if (ent.Comp.HailLevels != null && ent.Comp.HailLevels.Count > 1)
        {
            var user = args.User;

            args.Verbs.Add(new AlternativeVerb()
            {
                Text = Loc.GetString("hailer-gas-mask-verb"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")), //Cog icon
                Act = () =>
                {
                    UseVerbSwitchAggression(ent, user);
                }
            });
        }
    }

    private void UseVerbSwitchAggression(Entity<HailerComponent> ent, EntityUid userActed)
    {
        //Timer for verb cooldown
        ent.Comp.TimeVerbReady = _gameTiming.CurTime + ent.Comp.VerbCooldown;
        Dirty(ent);

        //Does the user has sufficient access level (security) ? If not, beep and show error
        if (!_access.IsAllowed(userActed, ent.Owner))
        {
            _audio.PlayPredicted(ent.Comp.SettingError, ent.Owner, ent.Owner, AudioParams.Default.WithVariation(0.15f));
            _popup.PopupPredicted(Loc.GetString("hailer-gas-mask-wrong_access"), Loc.GetString("hailer-gas-mask-wrong_access"), ent.Owner, userActed);
            return;
        }

        //If all good
        if (!HasComp<EmaggedComponent>(ent) && !ent.Comp.AreWiresCut)
        {
            _audio.PlayPredicted(ent.Comp.SettingBeep, ent.Owner, userActed, AudioParams.Default.WithVolume(0.5f).WithVariation(0.15f));
            IncreaseAggressionLevel(ent, userActed);
            Dirty(ent);
        }
    }

    private void OnHailAction(Entity<HailerComponent> ent, ref HailerActionEvent ev)
    {
        if (ev.Handled)
            return;

        if (TryComp<MaskComponent>(ent, out var mask))
        {
            if (!mask.IsToggled && !ent.Comp.AreWiresCut)
            {
                if (!_ui.IsUiOpen(ent.Owner, HailerUiKey.Key))
                {
                    if (_ui.TryOpenUi(ent.Owner, HailerUiKey.Key, ev.Performer))
                    {
                        // This is kinda bad as it starts the cooldown when the BUI opens instead of when choosing an option in the radial menu.
                        ev.Handled = true;
                    }
                }
            }
        }
    }

    protected virtual void OnHailOrder(Entity<HailerComponent> ent, ref HailerOrderMessage args)
    {
    }

    /// <summary>
    /// Put an exclamation mark around humanoid standing at the distance specified in the component.
    /// </summary>
    /// <param name="ent"></param>
    /// <returns>Is it handled succesfully ?</returns>
    private void ExclamateHumanoidsAround(Entity<HailerComponent> ent)
    {
        var (uid, comp) = ent;
        if (!Resolve(uid, ref comp, false) || comp.Distance <= 0)
            return;

        StealthComponent? stealth = null;
        foreach (var iterator in
            _entityLookup.GetEntitiesInRange<HumanoidAppearanceComponent>(_transform.GetMapCoordinates(uid), comp.Distance))
        {
            //Avoid pinging invisible entities
            if (TryComp(iterator, out stealth) && stealth.Enabled)
                continue;

            //We don't want to ping user of the mask
            if (iterator.Owner == ent.Comp.User)
                continue;

            SpawnAttachedTo(comp.ExclamationEffect, iterator.Owner.ToCoordinates());
        }
    }

    /// <summary>
    /// Increase the current index of HailLevels on the component
    /// Used for switching aggression level, changing the loc stirng and audio used for the one appropriate for the current HailLevel
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="clientUser">User who is increasing the level</param>
    private void IncreaseAggressionLevel(Entity<HailerComponent> ent, EntityUid clientUser)
    {
        //Do we have actual levels ?
        if (ent.Comp.HailLevels != null)
        {
            //Up the aggression level or reset it
            ent.Comp.HailLevelIndex++;
            if (ent.Comp.HailLevelIndex >= ent.Comp.HailLevels.Count)
                ent.Comp.HailLevelIndex = 0;

            //Notify player
            if (ent.Comp.CurrentHailLevel.HasValue)
                _popup.PopupPredicted(Loc.GetString("hailer-gas-mask-screwed", ("level", ent.Comp.CurrentHailLevel.Value.Name.ToLower())), ent.Owner, clientUser);
        }
    }

    /// <summary>
    /// Send a chat message as the hailer
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="localeText">What message ?</param>
    /// <param name="index">Index of the soundCollection currently selected</param>
    protected void SubmitChatMessage(Entity<HailerComponent> ent, string localeText, int index)
    {
        //Put the exclamations mark around people at the distance specified in the comp side
        //Just like a whistle
        ExclamateHumanoidsAround(ent);

        //Make a chat line with the sec hailer as speaker, in bold and UPPERCASE for added impact
        string sentence = Loc.GetString(localeText + "-" + index);

        _chat.TrySendInGameICMessage(ent.Owner, //Hailer, not user
                                    sentence.ToUpper(),
                                    InGameICChatType.Speak,
                                    hideChat: true,
                                    hideLog: true,
                                    nameOverride: ent.Comp.ChatName,
                                    checkRadioPrefix: false,
                                    ignoreActionBlocker: true);
    }


    private void OnInteractUsing(Entity<HailerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !ent.Comp.IsToolInteractible)
            return;

        //If someone else is wearing it and someone use a tool on it, ignore it
        //Strip system takes over
        if (ent.Comp.User.HasValue && ent.Comp.User != args.User)
            return;

        //Is it a wirecutter or a screwdriver ?
        if (_tool.HasQuality(args.Used, SharedToolSystem.CutQuality))
        {
            OnInteractCutting(ent, ref args);
            args.Handled = true;
        }
        else if (_tool.HasQuality(args.Used, SharedToolSystem.ScrewQuality))
        {
            OnInteractScrewing(ent, ref args);
            args.Handled = true;
        }

        return;
    }
    private void OnInteractCutting(Entity<HailerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        ProtoId<ToolQualityPrototype> quality = SharedToolSystem.CutQuality;
        StartADoAfter(ent, args, quality);
    }

    private void OnInteractScrewing(Entity<HailerComponent> ent, ref InteractUsingEvent args)
    {
        //If it's emagged we don't change it
        if (HasComp<EmaggedComponent>(ent) || ent.Comp.AreWiresCut || args.Handled)
            return;

        ProtoId<ToolQualityPrototype> quality = SharedToolSystem.ScrewQuality;
        StartADoAfter(ent, args, quality);
    }

    private void StartADoAfter(Entity<HailerComponent> ent, InteractUsingEvent args, ProtoId<ToolQualityPrototype> quality)
    {
        float? time = null;
        //What delay to use ?
        if (quality == SharedToolSystem.ScrewQuality)
        {
            time = ent.Comp.ScrewingDoAfterDelay;
        }
        else if (quality == SharedToolSystem.CutQuality)
        {
            time = ent.Comp.CuttingDoAfterDelay;
        }

        if (!time.HasValue)
        {
            Log.Error("SharedHailerSystem couldn't get a tool doAfter delay to use !");
            return;
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, time.Value, new HailerToolDoAfterEvent(quality), ent.Owner, target: args.Target, used: args.Used)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnToolDoAfter(Entity<HailerComponent> ent, ref HailerToolDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !HasComp<HailerComponent>(args.Args.Target))
            return;

        args.Handled = true;

        switch (args.ToolQuality)
        {
            case SharedToolSystem.CutQuality:
                OnCuttingDoAfter(ent, ref args);
                break;
            case SharedToolSystem.ScrewQuality:
                OnScrewingDoAfter(ent, ref args);
                break;
        }

        Dirty(ent);
    }

    private void OnCuttingDoAfter(Entity<HailerComponent> ent, ref HailerToolDoAfterEvent args)
    {
        _audio.PlayPredicted(ent.Comp.CutSounds, ent.Owner, args.User);

        var (uid, comp) = ent;

        //Toggle wires
        comp.AreWiresCut = !comp.AreWiresCut;
        Dirty(ent);

        //Change sprite, set in the yaml
        var state = comp.AreWiresCut ? "WiresCut" : "Intact";
        _appearance.SetData(ent, SecMaskVisuals.State, state);
        Dirty(ent);
    }

    private void OnScrewingDoAfter(Entity<HailerComponent> ent, ref HailerToolDoAfterEvent args)
    {
        _audio.PlayPredicted(ent.Comp.ScrewedSounds, ent.Owner, args.User);
        IncreaseAggressionLevel(ent, args.User);
    }
}
