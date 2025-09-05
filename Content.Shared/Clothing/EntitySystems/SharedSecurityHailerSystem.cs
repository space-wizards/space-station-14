using Content.Shared.Access.Systems;
using Content.Shared.Actions;
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
using System.Collections.Generic;

namespace Content.Shared.Clothing.EntitySystems;

public abstract class SharedSecurityHailerSystem : EntitySystem
{
    private const string CUTTING_QUALITY = "Cutting";
    private const string SCREWING_QUALITY = "Screwing";

    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _sharedAudio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HailerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HailerComponent, ClothingGotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<HailerComponent, ClothingGotUnequippedEvent>(OnUnequip);
        SubscribeLocalEvent<HailerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<HailerComponent, SecHailerToolDoAfterEvent>(OnToolDoAfter);
        SubscribeLocalEvent<HailerComponent, GotEmaggedEvent>(OnEmagging);
        SubscribeLocalEvent<HailerComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HailerComponent, ToggleMaskEvent>(OnToggleMask);
        SubscribeLocalEvent<HailerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    private void OnEquip(Entity<HailerComponent> ent, ref ClothingGotEquippedEvent args)
    {
        var (uid, comp) = ent;

        ent.Comp.User = args.Wearer;

        if (comp.CurrentState != SecMaskState.Functional)
            return;

        _actions.AddAction(args.Wearer, ref comp.ActionEntity, comp.Action, uid);
    }

    private void OnUnequip(Entity<HailerComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        var (uid, comp) = ent;

        if (comp.CurrentState != SecMaskState.Functional || !ent.Comp.User.HasValue)
            return;
        _actions.RemoveAction(ent.Comp.User.Value, comp.ActionEntity);
        ent.Comp.User = null;
    }

    //In case someone spawns with it ?
    private void OnMapInit(Entity<HailerComponent> ent, ref MapInitEvent args)
    {
        var (uid, comp) = ent;

        if (comp.CurrentState == SecMaskState.Functional)
            _actions.AddAction(uid, ref comp.ActionEntity, comp.Action);

        ent.Comp.ChatName ??= Loc.GetString("sec-hailer-default-chat-name");
        Dirty(uid, comp);
    }

    /// <summary>
    /// Put an exclamation mark around humanoid standing at the distance specified in the component.
    /// </summary>
    /// <param name="ent"></param>
    /// <returns>Is it handled succesfully ?</returns>
    protected bool ExclamateHumanoidsAround(Entity<HailerComponent> ent) //Put in shared for predictions purposes
    {
        var (uid, comp) = ent;
        if (!Resolve(uid, ref comp, false) || comp.Distance <= 0)
            return false;

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

        return true;
    }

    private void OnInteractUsing(Entity<HailerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        //If someone else is wearing it and someone use a tool on it, ignore it
        //Strip system takes over
        if (ent.Comp.User.HasValue && ent.Comp.User != args.User)
            return;

        //If ERT, can't be messed with
        if (ent.Comp.IsERT)
        {
            _popup.PopupEntity(Loc.GetString("ert-gas-mask-impossible"), ent.Owner);
            args.Handled = true;
            return;
        }

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
        if (HasComp<EmaggedComponent>(ent) || ent.Comp.CurrentState != SecMaskState.Functional)
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
            Log.Error("Security hailer system couldn't get a time for a tool doAfter !");
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
        if (args.Cancelled || args.Handled)
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
        // Snip, snip !
        _sharedAudio.PlayPvs(ent.Comp.CutSounds, ent.Owner);

        var (uid, comp) = ent;
        if (comp.CurrentState == SecMaskState.Functional)
        {
            comp.CurrentState = SecMaskState.WiresCut;
            if (ent.Comp.User.HasValue)
            {
                _actions.RemoveAction(ent.Comp.User.Value, comp.ActionEntity);
                Dirty(ent);
            }
        }
        else if (comp.CurrentState == SecMaskState.WiresCut)
        {
            comp.CurrentState = SecMaskState.Functional;
            if (ent.Comp.User.HasValue)
            {
                _actions.AddAction(ent.Comp.User.Value, ref comp.ActionEntity, comp.Action, uid);
                Dirty(ent);
            }
        }
        _appearance.SetData(ent, SecMaskVisuals.State, comp.CurrentState);
        args.Handled = true;
    }

    private void OnScrewingDoAfter(Entity<HailerComponent> ent, ref SecHailerToolDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !TryComp<HailerComponent>(args.Args.Target, out var plant))
            return;

        _sharedAudio.PlayPvs(ent.Comp.ScrewedSounds, ent.Owner);

        IncreaseAggressionLevel(ent);
        args.Handled = true;
    }

    private void IncreaseAggressionLevel(Entity<HailerComponent> ent)
    {
        //Up the aggression level by one or back to one
        if (ent.Comp.AggresionLevel == AggresionState.High)
            ent.Comp.AggresionLevel = AggresionState.Low;
        else
            ent.Comp.AggresionLevel++;

        _popup.PopupEntity(Loc.GetString("sec-gas-mask-screwed", ("level", ent.Comp.AggresionLevel.ToString().ToLower())), ent.Owner);
    }

    private void OnEmagging(Entity<HailerComponent> ent, ref GotEmaggedEvent args)
    {
        if (args.Handled || HasComp<EmaggedComponent>(ent))
            return;

        if (ent.Comp.User.HasValue && ent.Comp.User != args.UserUid)
            return;

        _popup.PopupEntity(Loc.GetString("sec-gas-mask-emagged"), ent.Owner);

        args.Type = EmagType.Interaction;

        Dirty(ent);
        args.Handled = true;

    }

    private void OnExamine(Entity<HailerComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.IsERT)
            args.PushMarkup(Loc.GetString("sec-gas-mask-examined-ert"));
        else if (HasComp<EmaggedComponent>(ent))
            args.PushMarkup(Loc.GetString("sec-gas-mask-examined-emagged"));
        else if (ent.Comp.CurrentState == SecMaskState.WiresCut)
            args.PushMarkup(Loc.GetString("sec-gas-mask-examined-wires-cut"));
        else
            args.PushMarkup(Loc.GetString($"sec-gas-mask-examined", ("level", ent.Comp.AggresionLevel)));
    }

    private void OnToggleMask(Entity<HailerComponent> ent, ref ToggleMaskEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp(ent.Owner, out MaskComponent? mask)
            && mask != null
            && ent.Comp.User.HasValue)
        {
            if (mask.IsToggled)
                _actions.RemoveAction(ent.Comp.User.Value, ent.Comp.ActionEntity);
            else if (ent.Comp.CurrentState == SecMaskState.Functional && ent.Comp.User.HasValue)
            {
                _actions.AddAction(ent.Comp.User.Value, ref ent.Comp.ActionEntity, ent.Comp.Action, ent.Owner);
            }
            Dirty(ent);
        }
    }

    private void OnGetVerbs(Entity<HailerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        //Cooldown to prevent spamming
        //Probably should put a cooldown effect on the mask to show the player, but no idea how to do that !
        if (_gameTiming.CurTime < ent.Comp.TimeVerbReady)
            return;

        if (!args.CanAccess || !args.CanInteract || ent.Comp.User != args.User)
            return;

        //If ERT, they don't switch aggression level
        if (ent.Comp.IsERT)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("sec-gas-mask-verb"),
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
            _popup.PopupEntity(Loc.GetString("sec-gas-mask-wrong_access"), userActed);
            return;
        }

        _sharedAudio.PlayPvs(ent.Comp.SettingBeep, ent.Owner, AudioParams.Default.WithVolume(0.5f).WithVariation(0.15f));
        IncreaseAggressionLevel(ent);
        Dirty(ent);
    }

    /// <summary>
    /// Play the compliance voice line  of the hailer
    /// </summary>
    /// <param name="ent"></param>
    /// <returns>Index of the chosen line from the SoundCollection</returns>
    protected int PlayVoiceLineSound(Entity<HailerComponent> ent)
    {
        //Move to shared for predictions purposes. Is this good ?
        var (uid, comp) = ent;

        SoundSpecifier currentSpecifier;
        if (comp.IsERT)
            currentSpecifier = comp.ERTAggressionSounds;
        else if (HasComp<EmaggedComponent>(ent))
            currentSpecifier = ent.Comp.EmagAggressionSounds;
        else
        {
            currentSpecifier = comp.AggresionLevel switch
            {
                AggresionState.Medium => comp.MediumAggressionSounds,
                AggresionState.High => comp.HighAggressionSounds,
                _ => comp.LowAggressionSounds,
            };
        }

        var resolver = _sharedAudio.ResolveSound(currentSpecifier);
        if (resolver is not ResolvedCollectionSpecifier collectionResolver)
            return -1;

        //Replace voice line
        if (comp.IsHOS && comp.AggresionLevel == AggresionState.High && collectionResolver.Index == comp.SecHailHighIndexForHOS) // MAGIC NUMBER !!!
        {
            //There is only one sound replacement at the moment, no need to check indexes
            resolver = (ResolvedCollectionSpecifier)_sharedAudio.ResolveSound(comp.HOSReplaceSounds);
        }

        _sharedAudio.PlayPvs(resolver, ent.Owner, audioParams: new AudioParams().WithVolume(-3f));

        return collectionResolver.Index;
    }

    /// <summary>
    /// Get the locale string format of the index given based on the context of the hailer
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    protected string GetLineFormat(Entity<HailerComponent> ent, int index)
    {
        string finalLine = String.Empty;
        if (HasComp<EmaggedComponent>(ent))
            finalLine = $"hail-emag-{index}";
        else if (ent.Comp.IsERT)
            finalLine = $"hail-ERT-{index}";
        else
            finalLine = $"hail-{ent.Comp.AggresionLevel.ToString().ToLower()}-{index}";

        //In case of replacement for HOS
        if (ent.Comp.IsHOS && ent.Comp.ReplaceVoicelinesLocalizeForHOS.ContainsKey(finalLine))
            finalLine = ent.Comp.ReplaceVoicelinesLocalizeForHOS[finalLine];

        return finalLine;
    }
}
