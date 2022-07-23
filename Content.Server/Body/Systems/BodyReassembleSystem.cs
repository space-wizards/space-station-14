using System.Threading;
using Content.Server.Body.Components;
using Content.Server.Cloning;
using Content.Server.DoAfter;
using Content.Server.Mind.Components;
using Content.Server.Popups;
using Content.Server.Preferences.Managers;
using Content.Shared.Actions;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Content.Shared.Species;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

/// <remarks>
/// Fair warning, this is all kinda shitcode, but it'll have to wait for a major
/// refactor until proper body systems get added. The current implementation is
/// definitely not ideal and probably will be prone to weird bugs.
/// </remarks>

namespace Content.Server.Body.Systems
{
    public sealed class BodyReassembleSystem : EntitySystem
    {
        [Dependency] private readonly IServerPreferencesManager _prefsManager = null!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;

        private const float SelfReassembleMultiplier = 2f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BodyReassembleComponent, PartGibbedEvent>(OnPartGibbed);
            SubscribeLocalEvent<BodyReassembleComponent, ReassembleActionEvent>(StartReassemblyAction);

            SubscribeLocalEvent<BodyReassembleComponent, GetVerbsEvent<AlternativeVerb>>(AddReassembleVerbs);
            SubscribeLocalEvent<BodyReassembleComponent, ReassembleCompleteEvent>(ReassembleComplete);
            SubscribeLocalEvent<BodyReassembleComponent, ReassembleCancelledEvent>(ReassembleCancelled);
        }

        private void StartReassemblyAction(EntityUid uid, BodyReassembleComponent component, ReassembleActionEvent args)
        {
            args.Handled = true;
            StartReassembly(uid, component, SelfReassembleMultiplier);
        }

        private void ReassembleCancelled(EntityUid uid, BodyReassembleComponent component, ReassembleCancelledEvent args)
        {
            component.CancelToken = null;
        }

        private void OnPartGibbed(EntityUid uid, BodyReassembleComponent component, PartGibbedEvent args)
        {
            if (!TryComp<MindComponent>(args.EntityToGib, out var mindComp) || mindComp?.Mind == null)
                return;

            component.BodyParts = args.GibbedParts;
            UpdateDNAEntry(uid, args.EntityToGib);
            mindComp.Mind.TransferTo(uid);

            if (component.ReassembleAction == null)
                return;

            _actions.AddAction(uid, component.ReassembleAction, null);
        }

        private void StartReassembly(EntityUid uid, BodyReassembleComponent component, float multiplier = 1f)
        {
            if (component.CancelToken != null)
                return;

            if (!GetNearbyParts(uid, component, out var partList))
                return;

            if (partList == null)
                return;

            var doAfterTime = component.DoAfterTime * multiplier;
            var cancelToken = new CancellationTokenSource();
            component.CancelToken = cancelToken;

            var doAfterEventArgs = new DoAfterEventArgs(component.Owner, doAfterTime, cancelToken.Token, component.Owner)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = false,
                TargetCancelledEvent = new ReassembleCancelledEvent(),
                TargetFinishedEvent = new ReassembleCompleteEvent(uid, uid, partList),
            };

            _doAfterSystem.DoAfter(doAfterEventArgs);
        }

        /// <summary>
        /// Adds the custom verb for reassembling body parts
        /// </summary>
        private void AddReassembleVerbs(EntityUid uid, BodyReassembleComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!TryComp<MindComponent>(uid, out var mind) ||
                !mind.HasMind ||
                component.CancelToken != null)
                return;

            // doubles the time if you reconstruct yourself
            var multiplier = args.User == uid ? SelfReassembleMultiplier : 1f;

            // Custom verb
            AlternativeVerb custom = new()
            {
                Text = Loc.GetString("reassemble-action"),
                Act = () =>
                {
                    StartReassembly(uid, component, multiplier);
                },
                IconEntity = uid,
                Priority = 1
            };
            args.Verbs.Add(custom);
        }

        private bool GetNearbyParts(EntityUid uid, BodyReassembleComponent component, out HashSet<EntityUid>? partList)
        {
            partList = new HashSet<EntityUid>();

            if (component.BodyParts == null)
                return false;

            // Ensures all of the old body part pieces are there
            var xformQuery = GetEntityQuery<TransformComponent>();
            var bodyXform = xformQuery.GetComponent(uid);

            foreach (var part in component.BodyParts)
            {
                if (!xformQuery.TryGetComponent(part, out var xform) ||
                    !bodyXform.Coordinates.InRange(EntityManager, xform.Coordinates,2f))
                {
                    _popupSystem.PopupEntity(Loc.GetString("reassemble-fail"), uid, Filter.Entities(uid));
                    return false;
                }
                partList.Add(part);
            }
            return true;
        }

        private void ReassembleComplete(EntityUid uid, BodyReassembleComponent component, ReassembleCompleteEvent args)
        {
            component.CancelToken = null;

            if (component.DNA == null || component.BodyParts == null)
                return;

            // Creates the new entity and transfers the mind component
            var speciesProto = _prototype.Index<SpeciesPrototype>(component.DNA.Value.Profile.Species).Prototype;
            var mob = EntityManager.SpawnEntity(speciesProto, EntityManager.GetComponent<TransformComponent>(component.Owner).MapPosition);

            _humanoidAppearance.UpdateFromProfile(mob, component.DNA.Value.Profile);
            MetaData(mob).EntityName = component.DNA.Value.Profile.Name;

            if (TryComp<MindComponent>(uid, out var mindcomp) && mindcomp.Mind != null)
                mindcomp.Mind.TransferTo(mob);

            // Cleans up all the body part pieces
            foreach (var entity in component.BodyParts)
            {
                EntityManager.DeleteEntity(entity);
            }

            _popupSystem.PopupEntity(Loc.GetString("reassemble-success", ("user", Identity.Entity(mob, EntityManager))), mob, Filter.Entities(mob));
        }

        /// <summary>
        /// Called before the skeleton entity is gibbed in order to save
        /// the dna for reassembly later
        /// </summary>
        /// <param name="uid"> the entity that the player will transfer to</param>
        /// <param name="body"> the entity whose DNA is being saved</param>
        private void UpdateDNAEntry(EntityUid uid, EntityUid body)
        {
            if (!TryComp<BodyReassembleComponent>(uid, out var skelBodyComp) || !TryComp<MindComponent>(body, out var mindcomp))
                return;

            if (mindcomp.Mind == null)
                return;

            if (mindcomp.Mind.UserId == null)
                return;

            var profile = (HumanoidCharacterProfile) _prefsManager.GetPreferences(mindcomp.Mind.UserId.Value).SelectedCharacter;
            skelBodyComp.DNA = new ClonerDNAEntry(mindcomp.Mind, profile);
        }

        private sealed class ReassembleCompleteEvent : EntityEventArgs
        {
            /// <summary>
            /// The entity being reassembled
            /// </summary>
            public readonly EntityUid Uid;

            /// <summary>
            /// The user performing the reassembly
            /// </summary>
            public readonly EntityUid User;
            public readonly HashSet<EntityUid> PartList;

            public ReassembleCompleteEvent(EntityUid uid, EntityUid user, HashSet<EntityUid> partList)
            {
                Uid = uid;
                User = user;
                PartList = partList;
            }
        }

        private sealed class ReassembleCancelledEvent : EntityEventArgs {}
    }
}

public sealed class ReassembleActionEvent : InstantActionEvent { }
