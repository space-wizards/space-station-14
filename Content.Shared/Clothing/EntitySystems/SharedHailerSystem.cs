using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.Clothing.ActionEvent;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.Event;
using Content.Shared.Coordinates;
using Content.Shared.Disposal;
using Content.Shared.Disposal.Components;
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
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Collections.Generic;

namespace Content.Shared.Clothing.EntitySystems;

public abstract class SharedHailerSystem : EntitySystem
{
    private const string CUTTING_QUALITY = "Cutting";
    private const string SCREWING_QUALITY = "Screwing";

    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _sharedAudio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HailerComponent, ClothingGotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<HailerComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HailerComponent, ClothingGotUnequippedEvent>(OnUnequip);
        SubscribeLocalEvent<HailerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<HailerComponent, GotEmaggedEvent>(OnEmagging);
        SubscribeLocalEvent<HailerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<HailerComponent, SecHailerToolDoAfterEvent>(OnToolDoAfter);

        SubscribeLocalEvent<HailerComponent, HailerOrderMessage>(OnHailOrder);
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
        Entity<HailerComponent> ent = (uid, comp);
        string soundCollection;
        string localeText;

        var orderUsed = comp.Orders[args.Index];
        var hailLevel = comp.CurrentHailLevel.Name;
        soundCollection = orderUsed.SoundCollection + "-" + hailLevel;
        localeText = orderUsed.LocalePrefix + "-" + hailLevel;

        //Play voice etc...
        var index = PlayVoiceLineSound(ent, soundCollection);
        SubmitChatMessage(ent, localeText, index);
    }


    private int PlayVoiceLineSound(Entity<HailerComponent> ent, string soundCollection)
    {
        var specifier = new SoundCollectionSpecifier(soundCollection);
        var resolver = _sharedAudio.ResolveSound(specifier);
        if (resolver is ResolvedCollectionSpecifier collectionResolver)
        {
            _sharedAudio.PlayPvs(resolver, ent.Owner, audioParams: new AudioParams().WithVolume(-3f));
            return collectionResolver.Index;
        }
        else
            return 0;
    }

    protected virtual void SubmitChatMessage(Entity<HailerComponent> ent, string localeText, int index)
    {
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
        if (args.Handled)
            return;

        //If someone else is wearing it and someone use a tool on it, ignore it
        //Strip system takes over
        if (ent.Comp.User.HasValue && ent.Comp.User != args.User)
            return;

        //Is it a wirecutter, a screwdriver or an EMAG ?
        if (_tool.HasQuality(args.Used, SharedToolSystem.CutQuality))
            OnInteractCutting(ent, ref args);
        else if (_tool.HasQuality(args.Used, SharedToolSystem.ScrewQuality))
            OnInteractScrewing(ent, ref args);
        else
            return;
    }
    private void OnInteractCutting(Entity<HailerComponent> ent, ref InteractUsingEvent args)
    {
        ProtoId<ToolQualityPrototype> quality = CUTTING_QUALITY;
        StartADoAfter(ent, args, quality);
    }

    private void OnInteractScrewing(Entity<HailerComponent> ent, ref InteractUsingEvent args)
    {
        //If it's emagged we don't change it
        if (HasComp<EmaggedComponent>(ent) || ent.Comp.AreWiresCut)
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

        args.Handled = true;
    }

    private void OnToolDoAfter(Entity<HailerComponent> ent, ref SecHailerToolDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !HasComp<HailerComponent>(args.Args.Target))
            return;

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
        _sharedAudio.PlayPvs(ent.Comp.CutSounds, ent.Owner);

        var (uid, comp) = ent;

        comp.AreWiresCut = !comp.AreWiresCut;
        Dirty(ent);
        var state = comp.AreWiresCut ? "WiresCut" : "Intact";
        _appearance.SetData(ent, SecMaskVisuals.State, state);
        Dirty(ent);
        args.Handled = true;
    }

    private void OnScrewingDoAfter(Entity<HailerComponent> ent, ref SecHailerToolDoAfterEvent args)
    {
        _sharedAudio.PlayPvs(ent.Comp.ScrewedSounds, ent.Owner);

        IncreaseAggressionLevel(ent);
        args.Handled = true;
    }

    protected virtual void IncreaseAggressionLevel(Entity<HailerComponent> ent)
    {
    }

    private void OnEmagging(Entity<HailerComponent> ent, ref GotEmaggedEvent args)
    {
        if (args.Handled || HasComp<EmaggedComponent>(ent))
            return;

        if (ent.Comp.User.HasValue && ent.Comp.User != args.UserUid)
            return;

        _popup.PopupEntity(Loc.GetString("sechail-gas-mask-emagged"), ent.Owner);

        args.Type = EmagType.Interaction;

        Dirty(ent);
        args.Handled = true;

    }

    private void OnExamine(Entity<HailerComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<EmaggedComponent>(ent))
            args.PushMarkup(Loc.GetString("sechail-gas-mask-emag"));
        else if (ent.Comp.AreWiresCut)
            args.PushMarkup(Loc.GetString("sechail-gas-mask-wires-cut"));
        else
            args.PushMarkup(Loc.GetString($"sechail-gas-mask-examined", ("level", ent.Comp.CurrentHailLevel.Name)));
    }

    private void OnGetVerbs(Entity<HailerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        //Cooldown to prevent spamming
        if (_gameTiming.CurTime < ent.Comp.TimeVerbReady)
            return;

        if (!args.CanAccess || !args.CanInteract || ent.Comp.User != args.User)
            return;

        if (ent.Comp.HailLevels.Count <= 1)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("sechail-gas-mask-verb"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () =>
            {
                UseVerbSwitchAggression(ent, user);
            }
        });
    }

    private void UseVerbSwitchAggression(Entity<HailerComponent> ent, EntityUid userActed)
    {
        ent.Comp.TimeVerbReady = _gameTiming.CurTime + ent.Comp.VerbCooldown;

        if (!_access.IsAllowed(userActed, ent.Owner))
        {
            _sharedAudio.PlayPvs(ent.Comp.SettingError, ent.Owner, AudioParams.Default.WithVariation(0.15f));
            _popup.PopupEntity(Loc.GetString("sechail-gas-mask-wrong_access"), userActed);
            return;
        }

        _sharedAudio.PlayPvs(ent.Comp.SettingBeep, ent.Owner, AudioParams.Default.WithVolume(0.5f).WithVariation(0.15f));
        IncreaseAggressionLevel(ent);
        Dirty(ent);
    }
}
