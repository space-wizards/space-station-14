using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Humanoid;
using Content.Server.Mind;
using Content.Server.Pointing.Components;
using Content.Server.Preferences.Managers;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Cloning;
using Content.Shared.Examine;
using Content.Shared.Eye;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
// yes, all of these are really necessary. Christ almighty.

namespace Content.Server._Impstation.MindlessClone;

public sealed class MindlessCloneSystem : EntitySystem
{
    // interfaces and managers
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly ISerializationManager _serManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;

    // everything else
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly RotateToFaceSystem _rotateToFaceSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindlessCloneComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MindlessCloneComponent, ExaminedEvent>(OnExamined);
    }

    /// <summary>
    /// Handles the "DoAfter"s. 
    /// </summary>
    public override void Update(float frametime)
    {
        base.Update(frametime);

        var query = EntityQueryEnumerator<MindlessCloneComponent>();

        while (query.MoveNext(out var uid, out var mindlessCloneComp))
        {
            if (mindlessCloneComp.NextDelayTime != null && _gameTiming.CurTime > mindlessCloneComp.NextDelayTime) // the null check is faster than the comparison, so it saves us some perf when we don't need the comparison anymore.
            {
                DelayBehavior(uid, mindlessCloneComp);
                var sayTime = TimeSpan.FromSeconds(0.1 * mindlessCloneComp.NextPhrase.Length);
                mindlessCloneComp.NextSayTime = _gameTiming.CurTime + sayTime;

                // and prevent it from happening again.
                mindlessCloneComp.NextDelayTime = null;
            }
            if (mindlessCloneComp.NextSayTime != null && _gameTiming.CurTime > mindlessCloneComp.NextSayTime)
            {
                SayBehavior(uid, mindlessCloneComp);

                // and prevent it from happening again.
                mindlessCloneComp.NextSayTime = null;
            }
        }
    }

    private void OnMapInit(Entity<MindlessCloneComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid) || !string.IsNullOrEmpty(humanoid.Initial))
            return;

        var cloneCoords = _transformSystem.GetMapCoordinates(ent.Owner);
        if (!TryGetNearestHumanoid(cloneCoords, out var target) || target == null) // grab the appearance data of the nearest humanoid with a mind.
            return;

        var targetUid = (EntityUid)target; // have to explicitly cast out of nullable

        ent.Comp.IsCloneOf = targetUid;

        // clone the appearance and components of the original
        if (!TryCloneNoOverwrite(targetUid, ent, ent.Comp.SettingsId))
        {
            Log.Error($"MindlessCloneSystem failed to clone {ToPrettyString(targetUid)} onto {ToPrettyString(ent)}. Message @widgetbeck on discord about it.");
            return;
        }

        // do our mindswapping logic before trying to speak, because otherwise the player starts the doafter but doesn't finish it.
        if (ent.Comp.MindSwap)
        {
            if (!_mind.TryGetMind(ent, out var cloneMind, out _) & !_mind.TryGetMind(ent.Comp.IsCloneOf, out var targetMind, out _))
                return;

            ent.Comp.MindSwap = false; // we don't want an infinite loop.
            ent.Comp.OriginalBody = ent.Owner;

            // now we copy the MindlessClone component over to our new host.
            if (HasComp<MindlessCloneComponent>(targetUid)) // it damn well shouldn't.
                RemComp<MindlessCloneComponent>(targetUid);
            CopyComp(ent, targetUid, ent.Comp);
            RemCompDeferred(ent, ent.Comp);

            ent.Comp.SpeakOnSpawn = false; // prevent the clone body from speaking before its MindlessCloneComponent gets a chance to be removed.

            // ... and swap all our vital components over to the new body.
            foreach (var componentName in ent.Comp.ComponentsToSwap)
            {
                if (!_componentFactory.TryGetRegistration(componentName, out var componentRegistration))
                {
                    Log.Error($"Tried to use invalid component registration for MindlessClone mind-swapping: {componentName}");
                    continue;
                }

                // if either or both sides have it, copy it over to the other side, and then remove it from yourself.
                if (_entityManager.TryGetComponent(targetUid, componentRegistration.Type, out var sourceComp)
                | _entityManager.TryGetComponent(ent, componentRegistration.Type, out var cloneComp))
                {
                    if (sourceComp != null && cloneComp != null) // if both sides have a component, we need to be clever about it.
                    {
                        // create two new components of this type
                        var sourceCopy = _componentFactory.GetComponent(componentRegistration);
                        var cloneCopy = _componentFactory.GetComponent(componentRegistration);

                        // copy the settings over to those
                        _serManager.CopyTo(sourceComp, ref sourceCopy, notNullableOverride: true);
                        _serManager.CopyTo(cloneComp, ref cloneCopy, notNullableOverride: true);

                        // remove the originals
                        RemComp(ent, componentRegistration.Type);
                        RemComp(targetUid, componentRegistration.Type);

                        // and copy the new components onto their respective ents
                        CopyComp(ent, targetUid, cloneCopy);
                        CopyComp(targetUid, ent, sourceCopy);
                    }
                    else if (sourceComp != null)
                    {
                        if (HasComp(ent, componentRegistration.Type))
                            RemComp(ent, componentRegistration.Type);
                        CopyComp(targetUid, ent, sourceComp);
                        RemComp(targetUid, componentRegistration.Type);
                    }
                    else if (cloneComp != null)
                    {
                        if (HasComp(targetUid, componentRegistration.Type))
                            RemComp(targetUid, componentRegistration.Type);
                        CopyComp(ent, targetUid, cloneComp);
                        RemComp(ent, componentRegistration.Type);
                    }
                }
            }

            // swap those minds around right quick
            _mind.TransferTo(cloneMind, targetUid); // technically we won't ever need to do this, it's just here for posterity.
            _mind.TransferTo(targetMind, ent); // this is the important one. 

            // and then hobble the target for a bit
            _statusEffect.TryAddStatusEffect<MutedComponent>(ent, "Muted", ent.Comp.MindSwapStunTime, true);
        }

        var isMindSwapped = ent.Owner == ent.Comp.IsCloneOf; // determine if we're operating on a mindswapped clone in the target body
        TimeSpan stunTime;

        if (isMindSwapped)
        {
            stunTime = TimeSpan.FromSeconds(0.1); // skip stunning the mindswappee - the delay doafter is just there for the stack
        }
        else
        {
            stunTime = ent.Comp.MindSwapStunTime;

            // make sure that the spawned clone isn't always facing north. they face the person they're a clone of, instead
            _rotateToFaceSystem.TryFaceCoordinates(ent, _transformSystem.ToMapCoordinates(Transform(ent.Comp.IsCloneOf).Coordinates).Position);

            _stun.TryParalyze(ent, stunTime, true);

            stunTime += TimeSpan.FromSeconds(0.5); // to make the delay end *after* the stun is up. otherwise the mobstate check fails
        }

        // delay starting the typing "DoAfter" until the clone (or the mindswapped original) is done being stunned.
        // we do this on mindswapped clones too because otherwise their TypingIndicators don't show up properly.
        ent.Comp.NextDelayTime = _gameTiming.CurTime + stunTime;
    }

    private void DelayBehavior(EntityUid uid, MindlessCloneComponent comp)
    {
        // if we're supposed to speak on spawn, try to speak on spawn. as long as you're not crit or dead
        if (comp.SpeakOnSpawn && !_mobState.IsIncapacitated(uid))
        {
            // enable the typing indicator for the duration of the DoAfter.
            _appearance.SetData(uid, TypingIndicatorVisuals.IsTyping, true);

            var choices = _prototypeManager.Index(comp.PhrasesToPick).Values;
            comp.NextPhrase = _random.Pick(choices);
        }
    }

    private void SayBehavior(EntityUid uid, MindlessCloneComponent comp)
    {
        if (uid == comp.IsCloneOf) // If we've mindswapped, behavior should be a little different.
        {
            _chat.TrySendInGameICMessage(uid,
                    Loc.GetString(comp.NextPhrase),
                    InGameICChatType.Speak,
                    hideChat: false,
                    hideLog: true,
                    checkRadioPrefix: false);
            // higher chance, but not guaranteed, to point at the original body.
            if (_random.Prob(0.6f))
                TryFakePoint(uid, comp.OriginalBody);
        }
        else
        {
            _chat.TrySendInGameICMessage(uid,
                Loc.GetString(comp.NextPhrase),
                InGameICChatType.Speak,
                hideChat: false,
                hideLog: true,
                checkRadioPrefix: false);
            // twenty percent chance to hit 2 after
            if (_random.Prob(0.2f))
                _chat.TrySendInGameICMessage(uid,
                    Loc.GetString("chat-emote-msg-scream"),
                    InGameICChatType.Emote,
                    hideChat: false,
                    hideLog: true,
                    checkRadioPrefix: false);
            // twenty percent chance to point at the original
            if (_random.Prob(0.2f))
                TryFakePoint(uid, comp.IsCloneOf);
        }

        // disable the typing indicator, as "typing" has now finished.
        _appearance.SetData(uid, TypingIndicatorVisuals.IsTyping, false);
    }

    /// <summary>
    /// Gets the nearest entity on the same map with HumanoidAppearanceComponent and a mind.
    /// </summary>
    private bool TryGetNearestHumanoid(MapCoordinates coordinates, [NotNullWhen(true)] out EntityUid? target)
    {
        target = null;
        var minDistance = float.PositiveInfinity;

        var enumerator = EntityQueryEnumerator<HumanoidAppearanceComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out _, out var xform))
        {
            if (coordinates.MapId != xform.MapID)
                continue;
            if (!_mind.TryGetMind(uid, out _, out _))
                continue;

            var coords = _transformSystem.GetWorldPosition(xform);
            var distanceSquared = (coordinates.Position - coords).LengthSquared();
            if (!float.IsInfinity(minDistance) && distanceSquared >= minDistance)
                continue;

            minDistance = distanceSquared;
            target = uid;
        }

        return target != null;
    }

    /// <summary>
    /// A near copy of CloningSystem.TryClone, except without the bit where it spawns a whole ass new entity (which was annoying)
    /// and with a new bit about copying over traits.
    /// </summary>
    public bool TryCloneNoOverwrite(EntityUid original, EntityUid clone, ProtoId<CloningSettingsPrototype> settingsId)
    {
        if (!TryComp<HumanoidAppearanceComponent>(original, out var originalAppearance))
            return false;

        HumanoidCharacterProfile profile;
        if (_mind.TryGetMind(original, out _, out var mindComponent) && mindComponent.Session != null)
        {
            // get the character profile of the humanoid out of its mind.
            var targetProfile = (HumanoidCharacterProfile)_prefs.GetPreferences(mindComponent.Session.UserId).SelectedCharacter;
            // clone it onto a new profile
            profile = new HumanoidCharacterProfile(targetProfile);
        }
        else // this shouldn't happen - TryGetNearestHumanoid should only be grabbing humanoids with minds.
        {
            profile = HumanoidCharacterProfile.DefaultWithSpecies(originalAppearance.Species)
            .WithSex(originalAppearance.Sex)
            .WithGender(originalAppearance.Gender);
        }

        if (!_prototypeManager.TryIndex(settingsId, out var settings)
            || !TryComp<HumanoidAppearanceComponent>(original, out var humanoid)
            || !_prototypeManager.TryIndex(humanoid.Species, out _))
            return false;

        _humanoid.CloneAppearance(original, clone);

        var componentsToCopy = settings.Components;

        if (TryComp<StatusEffectsComponent>(original, out var statusComp))
            componentsToCopy.ExceptWith(statusComp.ActiveEffects.Values.Select(s => s.RelevantComponent).Where(s => s != null)!);

        if (TryComp<NpcFactionMemberComponent>(original, out _))
            componentsToCopy.Remove("NpcFactionMember"); // we wanna make sure that we're not putting you and your evil clone on the same side.

        if (TryComp<VocalComponent>(original, out var vocalComp))
            vocalComp.ScreamActionEntity = null; // if i don't do this, VocalComponent errors upon being added to the clone, because CopyComp doesn't like attaching action entities.

        foreach (var componentName in componentsToCopy)
        {
            if (!_componentFactory.TryGetRegistration(componentName, out var componentRegistration))
            {
                Log.Error($"Tried to use invalid registration for MindlessClone cloning: {componentName}");
                continue;
            }

            if (_entityManager.TryGetComponent(original, componentRegistration.Type, out var sourceComp))
            {
                TryComp(clone, out MetaDataComponent? cloneMeta);

                if (HasComp(clone, componentRegistration.Type))
                    RemComp(clone, componentRegistration.Type);
                CopyComp(original, clone, sourceComp, cloneMeta);
            }
        }

        var originalName = Name(original);

        _metaData.SetEntityName(clone, originalName);

        // now we need to run some code to ensure traits get applied - since this isn't a player spawning, we need to basically duplicate the code from TraitSystem here, but without the bit that gives items.
        // No, I will not try to fix TraitSystem for this PR.
        if (profile.TraitPreferences.Count > 0)
        {
            foreach (var traitId in profile.TraitPreferences)
            {
                if (!_prototypeManager.TryIndex(traitId, out var traitPrototype))
                {
                    Log.Warning($"No trait found with ID {traitId}!");
                    continue;
                }

                if (_whitelistSystem.IsWhitelistFail(traitPrototype.Whitelist, clone) ||
                    _whitelistSystem.IsBlacklistPass(traitPrototype.Blacklist, clone))
                    continue;

                // Add all components required by the prototype to the body or specified organ
                if (traitPrototype.Organ != null)
                {
                    foreach (var organ in _bodySystem.GetBodyOrgans(clone))
                    {
                        if (traitPrototype.Organ is { } organTag && _tagSystem.HasTag(organ.Id, organTag))
                        {
                            EntityManager.AddComponents(organ.Id, traitPrototype.Components);
                        }
                    }
                }
                else
                {
                    EntityManager.AddComponents(clone, traitPrototype.Components, false);
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Custom examine text for when the clone is not crit or dead. 
    /// </summary>
    private void OnExamined(Entity<MindlessCloneComponent> ent, ref ExaminedEvent args)
    {
        if (_mobState.IsAlive(ent))
            args.PushMarkup($"{Loc.GetString("comp-mind-examined-mindlessclone", ("ent", ent.Owner))}");
    }

    /// <summary>
    /// This is largely a copy of PointingSystem.TryPoint. However, due to the way PointingSystem works, I can't just use the pointing events,
    /// on account of the clones not having a player session attached to them. Has no regard for range or blocking walls.
    /// </summary>

    private void TryFakePoint(EntityUid pointer, EntityUid pointee)
    {
        var coordsPointee = Transform(pointee).Coordinates;

        var mapCoordsPointee = _transformSystem.ToMapCoordinates(coordsPointee);
        _rotateToFaceSystem.TryFaceCoordinates(pointer, mapCoordsPointee.Position);

        var arrow = EntityManager.SpawnEntity("PointingArrow", coordsPointee);

        if (TryComp<PointingArrowComponent>(arrow, out var pointing))
        {
            pointing.StartPosition = _transformSystem.ToCoordinates((arrow, Transform(arrow)), _transformSystem.ToMapCoordinates(Transform(pointer).Coordinates)).Position;
            pointing.EndTime = _gameTiming.CurTime + TimeSpan.FromSeconds(4);

            Dirty(arrow, pointing);
        }

        var layer = (int)VisibilityFlags.Normal;
        if (TryComp(pointer, out VisibilityComponent? playerVisibility))
        {
            var arrowVisibility = EntityManager.EnsureComponent<VisibilityComponent>(arrow);
            layer = playerVisibility.Layer;
            _visibilitySystem.SetLayer((arrow, arrowVisibility), (ushort)layer);
        }

        // Get players that are in range and whose visibility layer matches the arrow's.
        bool ViewerPredicate(ICommonSession playerSession)
        {
            if (!_mind.TryGetMind(playerSession, out _, out var mind) ||
                mind.CurrentEntity is not { Valid: true } ent ||
                !TryComp(ent, out EyeComponent? eyeComp) ||
                (eyeComp.VisibilityMask & layer) == 0)
                return false;

            return _transformSystem.GetMapCoordinates(ent).InRange(_transformSystem.GetMapCoordinates(pointer), 15f);
        }

        var viewers = Filter.Empty()
            .AddWhere(session1 => ViewerPredicate(session1))
            .Recipients;

        var pointerName = Identity.Entity(pointer, EntityManager);
        var pointeeName = Identity.Entity(pointee, EntityManager);

        var viewerMessage = Loc.GetString("pointing-system-point-at-other-others", ("otherName", pointerName), ("other", pointeeName));
        var viewerPointedAtMessage = Loc.GetString("pointing-system-point-at-you-other", ("otherName", pointerName));

        SendMessage(pointer, viewers, pointee, "", viewerMessage, viewerPointedAtMessage);
    }

    /// <summary>
    /// Used in fake pointing for the popup messages.
    /// </summary>
    private void SendMessage(
        EntityUid source,
        IEnumerable<ICommonSession> viewers,
        EntityUid pointed,
        string selfMessage,
        string viewerMessage,
        string? viewerPointedAtMessage = null)
    {
        var netSource = GetNetEntity(source);

        foreach (var viewer in viewers)
        {
            if (viewer.AttachedEntity is not { Valid: true } viewerEntity)
            {
                continue;
            }

            var message = viewerEntity == source
                ? selfMessage
                : viewerEntity == pointed && viewerPointedAtMessage != null
                    ? viewerPointedAtMessage
                    : viewerMessage;

            // Someone pointing at YOU is slightly more important
            var popupType = viewerEntity == pointed ? PopupType.Medium : PopupType.Small;

            RaiseNetworkEvent(new PopupEntityEvent(message, popupType, netSource), viewerEntity);
        }

        _replay.RecordServerMessage(new PopupEntityEvent(viewerMessage, PopupType.Small, netSource));
    }
}
