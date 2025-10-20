using Content.Shared.Access.Systems;
using Content.Shared.Chasm;
using Content.Shared.Chat;
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
    private const string CUTTING_QUALITY = "Cutting";
    private const string SCREWING_QUALITY = "Screwing";

    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _sharedAudio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HailerComponent, ClothingGotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<HailerComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HailerComponent, ClothingGotUnequippedEvent>(OnUnequip);
        SubscribeLocalEvent<HailerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<HailerComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<HailerComponent, HailerOrderMessage>(OnHailOrder);
        SubscribeLocalEvent<HailerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<HailerComponent, SecHailerToolDoAfterEvent>(OnToolDoAfter);
    }


    private void OnEquip(Entity<HailerComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ent.Comp.User = args.Wearer;
    }
    private void OnUnequip(Entity<HailerComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        ent.Comp.User = null;
    }

    private void OnHailOrder(EntityUid uid, HailerComponent comp, HailerOrderMessage args)
    {
        string soundCollection;
        string localeText;
        Entity<HailerComponent> ent = (uid, comp);

        if (HasComp<EmaggedComponent>(ent) && comp.EmagLevelPrefix != null)
        {
            localeText = soundCollection = comp.EmagLevelPrefix;
        }
        else
        {
            var orderUsed = comp.Orders[args.Index];
            var hailLevel = comp.CurrentHailLevel != null ? "-" + comp.CurrentHailLevel.Value.Name : String.Empty;
            soundCollection = orderUsed.SoundCollection + hailLevel;
            localeText = orderUsed.LocalePrefix + hailLevel;
        }

        //Play voice etc...
        var index = PlayVoiceLineSound((uid, comp), soundCollection);
        SubmitChatMessage((uid, comp), localeText, index);
    }

    private int PlayVoiceLineSound(Entity<HailerComponent> ent, string soundCollection)
    {
        var specifier = new SoundCollectionSpecifier(soundCollection);
        var resolver = _sharedAudio.ResolveSound(specifier);
        if (resolver is ResolvedCollectionSpecifier collectionResolver)
        {
            //var foo = SoundSpecifier
            //_sharedAudio.PlayPredicted(resolver, ent.Owner, ent.Owner, audioParams: new AudioParams().WithVolume(-3f));
            return collectionResolver.Index;
        }
        else
            return 0;
    }

    protected void SubmitChatMessage(Entity<HailerComponent> ent, string localeText, int index)
    {
        if (ent.Comp.User.HasValue)
        {
            //Put the exclamations mark around people at the distance specified in the comp side
            //Just like a whistle
            ExclamateHumanoidsAround(ent);

            //Make a chat line with the sec hailer as speaker, in bold and UPPERCASE for added impact
            string sentence = Loc.GetString(localeText + "-" + index);

            _chat.TrySendInGameICMessage(ent.Comp.User.Value, //We submit the message via the User instead of the hailer to make the text BOLD
                                        sentence.ToUpper(),
                                        InGameICChatType.Speak,
                                        hideChat: true,
                                        hideLog: true,
                                        nameOverride: ent.Comp.ChatName,
                                        checkRadioPrefix: false,
                                        ignoreActionBlocker: true,
                                        skipTransform: true);
        }
        else
            Log.Error("SharedHailerSystem tried to send a chat message but the hailer had no user !");
    }

    /// <summary>
    /// Put an exclamation mark around humanoid standing at the distance specified in the component.
    /// </summary>
    /// <param name="ent"></param>
    /// <returns>Is it handled succesfully ?</returns>
    protected void ExclamateHumanoidsAround(Entity<HailerComponent> ent)
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

        ProtoId<ToolQualityPrototype> quality = CUTTING_QUALITY;
        StartADoAfter(ent, args, quality);
    }

    private void OnInteractScrewing(Entity<HailerComponent> ent, ref InteractUsingEvent args)
    {
        //If it's emagged we don't change it
        if (HasComp<EmaggedComponent>(ent) || ent.Comp.AreWiresCut || args.Handled)
            return;

        ProtoId<ToolQualityPrototype> quality = SCREWING_QUALITY;
        StartADoAfter(ent, args, quality);
    }

    private void StartADoAfter(Entity<HailerComponent> ent, InteractUsingEvent args, ProtoId<ToolQualityPrototype> quality)
    {
        float? time = null;
        //What delay to use ?
        if (quality == SCREWING_QUALITY)
        {
            time = ent.Comp.ScrewingDoAfterDelay;
        }
        else if (quality == CUTTING_QUALITY)
        {
            time = ent.Comp.CuttingDoAfterDelay;
        }

        if (!time.HasValue)
        {
            Log.Error("SharedHailerSystem couldn't get a time for a tool doAfter !");
            return;
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, time.Value, new SecHailerToolDoAfterEvent(quality), ent.Owner, target: args.Target, used: args.Used)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnToolDoAfter(Entity<HailerComponent> ent, ref SecHailerToolDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !HasComp<HailerComponent>(args.Args.Target))
            return;

        args.Handled = true;

        switch (args.ToolQuality)
        {
            case CUTTING_QUALITY:
                OnCuttingDoAfter(ent, ref args);
                break;
            case SCREWING_QUALITY:
                OnScrewingDoAfter(ent, ref args);
                break;
        }

        Dirty(ent);
    }

    private void OnCuttingDoAfter(Entity<HailerComponent> ent, ref SecHailerToolDoAfterEvent args)
    {
        _sharedAudio.PlayPredicted(ent.Comp.CutSounds, ent.Owner, args.User);

        var (uid, comp) = ent;

        comp.AreWiresCut = !comp.AreWiresCut;
        Dirty(ent);

        var state = comp.AreWiresCut ? "WiresCut" : "Intact";
        _appearance.SetData(ent, SecMaskVisuals.State, state);
        Dirty(ent);
    }

    private void OnScrewingDoAfter(Entity<HailerComponent> ent, ref SecHailerToolDoAfterEvent args)
    {
        _sharedAudio.PlayPredicted(ent.Comp.ScrewedSounds, ent.Owner, args.User);
        IncreaseAggressionLevel(ent, args.User);
    }

    protected void IncreaseAggressionLevel(Entity<HailerComponent> ent, EntityUid clientUser)
    {
        if (ent.Comp.HailLevels != null)
        {
            //Up the aggression level or reset it
            ent.Comp.HailLevelIndex++;
            if (ent.Comp.HailLevelIndex >= ent.Comp.HailLevels.Count)
                ent.Comp.HailLevelIndex = 0;

            if (ent.Comp.CurrentHailLevel.HasValue)
                _popup.PopupPredicted(Loc.GetString("hailer-gas-mask-screwed", ("level", ent.Comp.CurrentHailLevel.Value.Name.ToLower())), ent.Owner, clientUser);
        }
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
                desc = Loc.GetString(ent.Comp.DescriptionLocale, ("level", ent.Comp.CurrentHailLevel.Value.Name));
            else
                desc = Loc.GetString(ent.Comp.DescriptionLocale);
            args.PushMarkup(desc);
        }
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
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
                Act = () =>
                {
                    UseVerbSwitchAggression(ent, user);
                }
            });
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

    private void UseVerbSwitchAggression(Entity<HailerComponent> ent, EntityUid userActed)
    {
        ent.Comp.TimeVerbReady = _gameTiming.CurTime + ent.Comp.VerbCooldown;
        Dirty(ent);

        if (!_access.IsAllowed(userActed, ent.Owner))
        {
            _sharedAudio.PlayPredicted(ent.Comp.SettingError, ent.Owner, ent.Owner, AudioParams.Default.WithVariation(0.15f));
            _popup.PopupPredicted(Loc.GetString("hailer-gas-mask-wrong_access"), Loc.GetString("hailer-gas-mask-wrong_access"), ent.Owner, userActed);
            return;
        }

        if (!HasComp<EmaggedComponent>(ent) && !ent.Comp.AreWiresCut)
        {
            _sharedAudio.PlayPredicted(ent.Comp.SettingBeep, ent.Owner, userActed, AudioParams.Default.WithVolume(0.5f).WithVariation(0.15f));
            IncreaseAggressionLevel(ent, userActed);
            Dirty(ent);
        }
    }
}
