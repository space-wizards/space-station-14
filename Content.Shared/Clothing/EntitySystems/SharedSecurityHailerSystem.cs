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
using Content.Shared.Swab;
using Content.Shared.Temperature.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing.EntitySystems
{
    public abstract class SharedSecurityHailerSystem : EntitySystem
    {
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
            SubscribeLocalEvent<SecurityHailerComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<SecurityHailerComponent, ClothingGotEquippedEvent>(OnEquip);
            SubscribeLocalEvent<SecurityHailerComponent, ClothingGotUnequippedEvent>(OnUnequip);
            SubscribeLocalEvent<SecurityHailerComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<SecurityHailerComponent, SecHailerToolDoAfterEvent>(OnToolDoAfter);
            SubscribeLocalEvent<SecurityHailerComponent, GotEmaggedEvent>(OnEmagging);
            SubscribeLocalEvent<SecurityHailerComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<SecurityHailerComponent, ToggleMaskEvent>(OnToggleMask);
            SubscribeLocalEvent<SecurityHailerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        }

        private void OnEquip(Entity<SecurityHailerComponent> ent, ref ClothingGotEquippedEvent args)
        {
            var (uid, comp) = ent;

            if (comp.CurrentState != SecMaskState.Functional)
                return;

            ent.Comp.User = args.Wearer;
            _actions.AddAction(args.Wearer, ref comp.ActionEntity, comp.Action, uid);
        }

        private void OnUnequip(Entity<SecurityHailerComponent> ent, ref ClothingGotUnequippedEvent args)
        {
            var (uid, comp) = ent;

            if (comp.CurrentState != SecMaskState.Functional)
                return;
            _actions.RemoveAction(ent.Comp.User, comp.ActionEntity);
            ent.Comp.User = EntityUid.Invalid;
        }

        //In case someone spawns with it ?
        private void OnMapInit(Entity<SecurityHailerComponent> ent, ref MapInitEvent args)
        {
            //COPY PASTED, IS THIS GOOD ?
            var (uid, comp) = ent;
            // test funny
            if (string.IsNullOrEmpty(comp.Action))
                return;

            if (comp.CurrentState == SecMaskState.Functional)
                _actions.AddAction(uid, ref comp.ActionEntity, comp.Action);
            Dirty(uid, comp);
        }

        /// <summary>
        /// Put an exclamation mark around humanoid standing at the distance specified in the component.
        /// </summary>
        /// <param name="ent"></param>
        /// <returns></returns>
        protected bool ExclamateHumanoidsAround(Entity<SecurityHailerComponent> ent) //Put in shared for predictions purposes
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

        private void OnInteractUsing(Entity<SecurityHailerComponent> ent, ref InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            //If someone else is wearing it and someone use a tool on it, ignore it
            if (ent.Comp.User != EntityUid.Invalid && ent.Comp.User != args.User)
                return;

            if (ent.Comp.SpecialCircumtance == SecurityHailerComponent.SpecialUseCase.ERT)
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
        private void OnInteractCutting(Entity<SecurityHailerComponent> ent, ref InteractUsingEvent args)
        {
            StartADoAfter(ent, args, SecHailerToolDoAfterEvent.ToolQuality.Cutting);
        }

        private void OnInteractScrewing(Entity<SecurityHailerComponent> ent, ref InteractUsingEvent args)
        {
            //If it's emagged we don't change it
            if (HasComp<EmaggedComponent>(ent) || ent.Comp.CurrentState != SecMaskState.Functional)
                return;
            StartADoAfter(ent, args, SecHailerToolDoAfterEvent.ToolQuality.Screwing);
        }

        private void StartADoAfter(Entity<SecurityHailerComponent> ent, InteractUsingEvent args, SecHailerToolDoAfterEvent.ToolQuality quality)
        {
            float? time = null;

            //What delay to use ?
            if (quality == SecHailerToolDoAfterEvent.ToolQuality.Screwing)
            {
                time = ent.Comp.ScrewingDoAfterDelay;
            }
            else if (quality == SecHailerToolDoAfterEvent.ToolQuality.Cutting)
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

        private void OnToolDoAfter(Entity<SecurityHailerComponent> ent, ref SecHailerToolDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled)
                return;

            switch (args.UsedTool)
            {
                case SecHailerToolDoAfterEvent.ToolQuality.Cutting:
                    OnCuttingDoAfter(ent, ref args);
                    break;
                case SecHailerToolDoAfterEvent.ToolQuality.Screwing:
                    OnScrewingDoAfter(ent, ref args);
                    break;
            }

            Dirty(ent);
        }

        private void OnCuttingDoAfter(Entity<SecurityHailerComponent> ent, ref SecHailerToolDoAfterEvent args)
        {
            // Snip, snip !
            _sharedAudio.PlayPvs(ent.Comp.CutSounds, ent.Owner);

            var (uid, comp) = ent;
            if (comp.CurrentState == SecMaskState.Functional)
            {
                comp.CurrentState = SecMaskState.WiresCut;
                if (ent.Comp.User != EntityUid.Invalid)
                {
                    _actions.RemoveAction(ent.Comp.User, comp.ActionEntity);
                }
                Dirty(ent);
            }
            else if (comp.CurrentState == SecMaskState.WiresCut)
            {
                comp.CurrentState = SecMaskState.Functional;
                if (ent.Comp.User != EntityUid.Invalid)
                {
                    _actions.AddAction(ent.Comp.User, ref comp.ActionEntity, comp.Action, uid);
                    Dirty(ent);
                }
            }
            _appearance.SetData(ent, SecMaskVisuals.State, comp.CurrentState);
            args.Handled = true;
        }

        private void OnScrewingDoAfter(Entity<SecurityHailerComponent> ent, ref SecHailerToolDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled || !TryComp<SecurityHailerComponent>(args.Args.Target, out var plant))
                return;

            var comp = ent.Comp;
            //Play a click sound just like the headset
            _sharedAudio.PlayPvs(ent.Comp.ScrewedSounds, ent.Owner);

            IncreaseAggressionLevel(ent);
            args.Handled = true;
        }

        private void IncreaseAggressionLevel(Entity<SecurityHailerComponent> ent)
        {
            //Up the aggression level by one or back to one
            if (ent.Comp.AggresionLevel == SecurityHailerComponent.AggresionState.High)
                ent.Comp.AggresionLevel = SecurityHailerComponent.AggresionState.Low;
            else
                ent.Comp.AggresionLevel++;

            _popup.PopupEntity(Loc.GetString("sec-gas-mask-screwed", ("level", ent.Comp.AggresionLevel.ToString().ToLower())), ent.Owner);
        }

        private void OnEmagging(Entity<SecurityHailerComponent> ent, ref GotEmaggedEvent args)
        {
            if (args.Handled || HasComp<EmaggedComponent>(ent))
                return;

            if (ent.Comp.User != EntityUid.Invalid && ent.Comp.User != args.UserUid)
                return;

            _popup.PopupEntity(Loc.GetString("sec-gas-mask-emagged"), ent.Owner);

            args.Type = EmagType.Interaction;

            Dirty(ent);
            args.Handled = true;

        }

        private void OnExamine(Entity<SecurityHailerComponent> ent, ref ExaminedEvent args)
        {
            if (ent.Comp.SpecialCircumtance == SecurityHailerComponent.SpecialUseCase.ERT)
                args.PushMarkup(Loc.GetString("sec-gas-mask-examined-ert"));
            else if (HasComp<EmaggedComponent>(ent))
                args.PushMarkup(Loc.GetString("sec-gas-mask-examined-emagged"));
            else if (ent.Comp.CurrentState == SecMaskState.WiresCut)
                args.PushMarkup(Loc.GetString("sec-gas-mask-examined-wires-cut"));
            else
                args.PushMarkup(Loc.GetString($"sec-gas-mask-examined", ("level", ent.Comp.AggresionLevel)));
        }

        private void OnToggleMask(Entity<SecurityHailerComponent> ent, ref ToggleMaskEvent args)
        {
            if (args.Handled)
                return;

            if (TryComp(ent.Owner, out MaskComponent? mask)
                && mask != null)
            {
                if (mask.IsToggled)
                    _actions.RemoveAction(ent.Comp.User, ent.Comp.ActionEntity);
                else if (ent.Comp.CurrentState == SecMaskState.Functional && ent.Comp.User != EntityUid.Invalid)
                {
                    _actions.AddAction(ent.Comp.User, ref ent.Comp.ActionEntity, ent.Comp.Action, ent.Owner);
                }
                Dirty(ent);
            }
        }

        private void OnGetVerbs(Entity<SecurityHailerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
        {
            //Cooldown to prevent spamming
            if (_gameTiming.CurTime < ent.Comp.TimeVerbReady)
                return;

            if (!args.CanAccess || !args.CanInteract || ent.Comp.User != args.User)
                return;

            //If ERT, they don't switch aggression level
            if (ent.Comp.SpecialCircumtance == SecurityHailerComponent.SpecialUseCase.ERT)
                return;

            // Can't pass args from a ref event inside of lambdas
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

        private void UseVerbSwitchAggression(Entity<SecurityHailerComponent> ent, EntityUid userActed)
        {
            ent.Comp.TimeVerbReady = _gameTiming.CurTime + ent.Comp.VerbCooldown;

            if (!_access.IsAllowed(userActed, ent.Owner))
            {
                _sharedAudio.PlayPvs(ent.Comp.SettingError, ent.Owner, AudioParams.Default.WithVariation(0.15f));
                _popup.PopupEntity(Loc.GetString("sec-gas-mask-wrong_access"), userActed);
                return;
            }

            _sharedAudio.PlayPvs(ent.Comp.SettingBeep, ent.Owner, AudioParams.Default.WithVariation(0.15f));
            IncreaseAggressionLevel(ent);
            Dirty(ent);
        }
    }
}
