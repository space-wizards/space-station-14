using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Coordinates;
using Content.Shared.Stealth.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Swab;
using Content.Shared.Clothing.Event;
using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;
using Content.Shared.Emag.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Examine;

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

        private EntityUid _wearer;

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
        }

        private void OnEquip(Entity<SecurityHailerComponent> ent, ref ClothingGotEquippedEvent args)
        {
            var (uid, comp) = ent;

            if (comp.CurrentState != SecMaskState.Functional)
                return;

            _wearer = args.Wearer;
            _actions.AddAction(args.Wearer, ref comp.ActionEntity, comp.Action, uid);
        }

        private void OnUnequip(Entity<SecurityHailerComponent> ent, ref ClothingGotUnequippedEvent args)
        {
            var (uid, comp) = ent;

            if (comp.CurrentState != SecMaskState.Functional)
                return;
            _actions.RemoveAction(_wearer, comp.ActionEntity);
            _wearer = EntityUid.Invalid;
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

                //We don't want to ping user of whistle
                if (iterator.Owner == _wearer)
                    continue;

                SpawnAttachedTo(comp.ExclamationEffect, iterator.Owner.ToCoordinates());
            }

            return true;
        }

        private void OnInteractUsing(Entity<SecurityHailerComponent> ent, ref InteractUsingEvent args)
        {
            if (args.Handled)
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
            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.ScrewingDoAfterDelay, new SecHailerToolDoAfterEvent(quality), ent.Owner, target: args.Target, used: args.Used)
            {
                Broadcast = true,
                BreakOnMove = true,
                NeedHand = true,
            });

            if (_wearer != EntityUid.Invalid)
            {
                _popup.PopupEntity(Loc.GetString("sec-gas-mask-alert-owner", ("user", args.User), ("quality", quality)), _wearer, _wearer, PopupType.LargeCaution);
            }

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
        }

        private void OnCuttingDoAfter(Entity<SecurityHailerComponent> ent, ref SecHailerToolDoAfterEvent args)
        {
            // Snip, snip !
            _sharedAudio.PlayPvs(ent.Comp.CutSounds, ent.Owner);

            var (uid, comp) = ent;
            if (comp.CurrentState == SecMaskState.Functional)
            {
                comp.CurrentState = SecMaskState.WiresCut;
                _actions.RemoveAction(_wearer, comp.ActionEntity);
                Dirty(ent);
            }
            else if (comp.CurrentState == SecMaskState.WiresCut)
            {
                comp.CurrentState = SecMaskState.Functional;
                if (_wearer != EntityUid.Invalid)
                {
                    _actions.AddAction(_wearer, ref comp.ActionEntity, comp.Action, uid);
                    Dirty(ent);
                }
            }
            _appearance.SetData(ent, SecMaskVisuals.State, comp.CurrentState);
            args.Handled = true;
        }

        private void OnScrewingDoAfter(Entity<SecurityHailerComponent> ent, ref SecHailerToolDoAfterEvent args)
        {
            //Play a click sound just like the headset
            _sharedAudio.PlayPvs(ent.Comp.ScrewedSounds, ent.Owner);

            if (args.Cancelled || args.Handled || !TryComp<SecurityHailerComponent>(args.Args.Target, out var plant))
                return;

            var comp = ent.Comp;

            //Up the aggression level by one or back to one
            if (comp.AggresionLevel == SecurityHailerComponent.AggresionState.High)
                comp.AggresionLevel = SecurityHailerComponent.AggresionState.Low;
            else
                comp.AggresionLevel++;

            _popup.PopupEntity(Loc.GetString("sec-gas-mask-screwed", ("level", ent.Comp.AggresionLevel.ToString().ToLower())), ent.Owner);
            args.Handled = true;
        }

        private void OnEmagging(Entity<SecurityHailerComponent> ent, ref GotEmaggedEvent args)
        {
            if (args.Handled || HasComp<EmaggedComponent>(ent))
                return;

            if (_wearer != EntityUid.Invalid && args.UserUid != _wearer)
                _popup.PopupEntity(Loc.GetString("sec-gas-mask-alert-owner-post-emag", ("user", args.UserUid)), _wearer, _wearer, PopupType.LargeCaution);

            _popup.PopupEntity(Loc.GetString("sec-gas-mask-emagged"), ent.Owner);

            args.Type = EmagType.Interaction;
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
                    _actions.RemoveAction(_wearer, ent.Comp.ActionEntity);
                else
                    _actions.AddAction(_wearer, ref ent.Comp.ActionEntity, ent.Comp.Action, ent.Owner);
            }
        }
    }
}
