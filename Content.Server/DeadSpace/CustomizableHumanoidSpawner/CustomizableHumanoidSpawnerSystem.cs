// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Diagnostics.CodeAnalysis;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared.DeadSpace.CustomizableHumanoidSpawner;
using Content.Shared.Mind.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Content.Shared.Speech;
using Content.Shared.Tag;

namespace Content.Server.DeadSpace.CustomizableHumanoidSpawner;

public sealed class CustomizableHumanoidSpawnerSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly StationSpawningSystem _spawning = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRole = default!;
    [Dependency] private readonly SpeechSystem _speech = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly NpcFactionSystem _factionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CustomizableHumanoidSpawnerComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<CustomizableHumanoidSpawnerComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<CustomizableHumanoidSpawnerComponent, CustomizableHumanoidSpawnerMessage>(OnUiMessage);
        SubscribeLocalEvent<CustomizableHumanoidSpawnerComponent, TakeGhostRoleEvent>(OnTakeGhostRole);
    }

    private void OnMindAdded(EntityUid uid, CustomizableHumanoidSpawnerComponent comp, MindAddedMessage args)
    {
        if (TryComp(uid, out GhostRoleComponent? ghost))
            _ghostRole.UnregisterGhostRole((uid, ghost));

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _speech.SetSpeech(uid, false);
        if (comp.ForceRandom || GetAvailableCharacters(actor, comp).Count == 0)
        {
            Spawn(
                uid,
                comp,
                actor,
                true,
                0,
                string.Empty,
                false,
                string.Empty);

            return;
        }

        _ui.OpenUi(uid, CustomizableHumanoidSpawnerUiKey.Key, actor.PlayerSession);
    }

    private void OnUiOpened(EntityUid uid, CustomizableHumanoidSpawnerComponent comp, BoundUIOpenedEvent args)
    {
        if (!TryComp<ActorComponent>(args.Actor, out var actor))
            return;

        var isRandomizedName = TryGetRandomName(comp, out var randomizedName);

        var state = new CustomizableHumanoidSpawnerBuiState(
            GetAvailableCharacters(actor, comp),
            !isRandomizedName,
            randomizedName,
            comp.AllowedSpecies);

        _ui.SetUiState(uid, CustomizableHumanoidSpawnerUiKey.Key, state);
    }

    private void OnUiMessage(EntityUid uid, CustomizableHumanoidSpawnerComponent comp, CustomizableHumanoidSpawnerMessage msg)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        Spawn(uid,
            comp,
            actor,
            msg.UseRandom,
            msg.CharacterIndex,
            msg.CustomName,
            msg.UseCustomDescription,
            msg.CustomDescription);
    }

    private void Spawn(EntityUid uid,
        CustomizableHumanoidSpawnerComponent comp,
        ActorComponent actor,
        bool isRandomCharacter,
        int characterIndex,
        string customName,
        bool useCustomDescription,
        string customDescription)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mindComp))
            return;

        if (!_transform.TryGetMapOrGridCoordinates(uid, out var coords))
            return;

        HumanoidCharacterProfile profile;
        if (isRandomCharacter)
        {
            if (comp.AllowedSpecies.Count == 0)
            {
                profile = HumanoidCharacterProfile.Random();
            }
            else
            {
                profile = HumanoidCharacterProfile.RandomWithSpecies(_random.Pick(comp.AllowedSpecies));
            }

            if (TryGetRandomName(comp, out var randomName))
                profile = profile.WithName(randomName);
        }
        else
        {
            var prefs = _prefs.GetPreferences(actor.PlayerSession.UserId);
            profile = (HumanoidCharacterProfile) prefs.GetProfile(characterIndex);
            profile = profile.WithName(customName);

            if (useCustomDescription)
                profile = profile.WithFlavorText(customDescription);
        }

        var newEntity = _spawning.SpawnPlayerMob(coords.Value, comp.JobPrototype, profile, null);

        if (comp.Tags != null)
            _tagSystem.AddTags(newEntity, comp.Tags);

        if (comp.Factions != null)
            _factionSystem.AddFactions(newEntity, comp.Factions);

        _mind.TransferTo(mindId, newEntity, true, mind: mindComp);

        QueueDel(uid);
    }

    private void OnTakeGhostRole(EntityUid uid, CustomizableHumanoidSpawnerComponent comp, ref TakeGhostRoleEvent args)
    {
        if (args.TookRole)
            return;

        args.TookRole = true;

        if (TryComp(uid, out GhostRoleComponent? ghost))
            _ghostRole.UnregisterGhostRole((uid, ghost));

        _mind.ControlMob(args.Player.UserId, uid);
    }

    private List<CustomizableHumanoidSpawnerCharacterInfo> GetAvailableCharacters(ActorComponent actor, CustomizableHumanoidSpawnerComponent comp)
    {
        var prefs = _prefs.GetPreferences(actor.PlayerSession.UserId);
        var characters = new List<CustomizableHumanoidSpawnerCharacterInfo>();

        foreach (var (index, profile) in prefs.Characters)
        {
            if (profile is not HumanoidCharacterProfile humanoid ||
                !comp.AllowedSpecies.Contains(humanoid.Species) && comp.AllowedSpecies.Count > 0)
                continue;

            var flavor = humanoid.FlavorText;
            characters.Add(new CustomizableHumanoidSpawnerCharacterInfo(
                humanoid.Name,
                flavor,
                humanoid.Species,
                humanoid.Gender,
                index));
        }

        return characters;
    }

    private bool TryGetRandomName(CustomizableHumanoidSpawnerComponent comp, [NotNullWhen(true)] out string? name)
    {
        var firstPart = TryGetRandomNameFirst(comp);
        var secondsPart = TryGetRandomNameSecond(comp);

        if (firstPart is null)
        {
            name = null;
            return false;
        }

        name = secondsPart is null ? firstPart : $"{firstPart} {secondsPart}";
        return true;
    }

    private string? TryGetRandomNameFirst(CustomizableHumanoidSpawnerComponent comp)
    {
        if (comp.RandomNameFirstDataset is not null)
            return _random.Pick(_prototypeManager.Index(comp.RandomNameFirstDataset.Value).Values);

        if (comp.RandomNameFirstLocalized is not null)
            return Loc.GetString(_random.Pick(_prototypeManager.Index(comp.RandomNameFirstLocalized.Value).Values));

        return null;
    }

    private string? TryGetRandomNameSecond(CustomizableHumanoidSpawnerComponent comp)
    {
        if (comp.RandomNameSecondDataset is not null)
            return _random.Pick(_prototypeManager.Index(comp.RandomNameSecondDataset.Value).Values);

        if (comp.RandomNameSecondLocalized is not null)
            return Loc.GetString(_random.Pick(_prototypeManager.Index(comp.RandomNameSecondLocalized.Value).Values));

        return null;
    }
}
