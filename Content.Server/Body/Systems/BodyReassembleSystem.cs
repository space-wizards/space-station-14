using System.Threading;
using Content.Server.Body.Components;
using Content.Server.DoAfter;
using Content.Server.Mind.Components;
using Content.Server.Popups;
using Content.Server.Preferences.Managers;
using Content.Shared.Actions;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

/// <remarks>
/// Fair warning, this is all kinda shitcode, but it'll have to wait for a major
/// refactor until proper body systems get added. The current implementation is
/// definitely not ideal and probably will be prone to weird bugs.
/// </remarks>

namespace Content.Server.Body.Systems;

public sealed class BodyReassembleSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

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
            MovementThreshold = 1f,
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

        var tempList = new HashSet<EntityUid>(component.BodyParts);
        tempList.Remove(component.Owner);
        partList.Add(component.Owner); //the query dislikes the owner

        var entq = GetEntityQuery<BodyPartComponent>();
        EntityUid? foundPart = null;
        foreach (var entity in _lookup.GetEntitiesInRange(component.Owner, 2f))
        {
            if (!entq.HasComponent(entity))
                continue;

            foreach (var part in tempList)
            {
                var partmeta = MetaData(part);
                var entmeta = MetaData(entity);

                if (partmeta.EntityPrototype == null || entmeta.EntityPrototype == null)
                    continue;

                if (partmeta.EntityPrototype.ID == entmeta.EntityPrototype.ID)
                {
                    partList.Add(entity);
                    foundPart = part;
                }
            }

            if (foundPart != null)
                tempList.Remove(foundPart.Value);

            foundPart = null;
        }

        if (tempList.Count == 0)
        {
            return true;
        }
        else
        {
            _popupSystem.PopupEntity(Loc.GetString("reassemble-fail"), uid, Filter.Pvs(uid));
            return false;
        }    
    }

    private void ReassembleComplete(EntityUid uid, BodyReassembleComponent component, ReassembleCompleteEvent args)
    {
        component.CancelToken = null;

        if (component.Profile == null || component.BodyParts == null || component.Profile.Prototype == null)
            return;

        // Creates the new entity and transfers the mind component
        var mob = EntityManager.SpawnEntity(component.Profile.Prototype, EntityManager.GetComponent<TransformComponent>(component.Owner).MapPosition);

        if (component.Profile.Appearance != null)
            _humanoidAppearance.UpdateAppearance(mob, component.Profile.Appearance);
        
        MetaData(mob).EntityName = component.Profile.Name;

        if (TryComp<MindComponent>(uid, out var mindcomp) && mindcomp.Mind != null)
            mindcomp.Mind.TransferTo(mob);

        // Cleans up all the body part pieces
        foreach (var entity in args.PartList)
            EntityManager.DeleteEntity(entity);

        _popupSystem.PopupEntity(Loc.GetString("reassemble-success", ("user", mob)), mob, Filter.Entities(mob));
    }

    /// <summary>
    /// Called before the entity is destroyed in order to save
    /// the dna for reassembly later
    /// </summary>
    /// <param name="uid"> the entity that the player will transfer to</param>
    /// <param name="body"> the entity whose DNA is being saved</param>
    private void UpdateDNAEntry(EntityUid uid, EntityUid body)
    {
        if (!TryComp<BodyReassembleComponent>(uid, out var bodyreassemble) ||
            !TryComp<MindComponent>(body, out var mind) ||
            !TryComp<MetaDataComponent>(body, out var meta))
            return;

        if (mind.Mind == null || mind.Mind.UserId == null || meta.EntityPrototype == null)
            return;

        HumanoidCharacterAppearance? emo = null;
        if (TryComp<HumanoidAppearanceComponent>(body, out var app))
        {
            emo = app.Appearance;
            //default skin color is uggo, come at me once HumanoidCharacterAppearance is refactored
            if (emo.SkinColor == Color.FromHex("#C0967F"))
                emo = emo.WithSkinColor(Color.White);
        }

        bodyreassemble.Profile = new ReassembleEntityProfile(meta.EntityPrototype.ID, meta.EntityName, emo);
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
    private sealed class ReassembleCancelledEvent : EntityEventArgs { }
}

public sealed class ReassembleActionEvent : InstantActionEvent { }

//This could probably be generalized for cloning or something but i'm not touching cloning rn
public sealed class ReassembleEntityProfile
{
    public readonly string Prototype;

    public readonly string Name;

    public readonly HumanoidCharacterAppearance? Appearance;

    public ReassembleEntityProfile(string prototype, string name, HumanoidCharacterAppearance? appearance)
    {
        Prototype = prototype;
        Name = name;
        Appearance = appearance;
    }
}
