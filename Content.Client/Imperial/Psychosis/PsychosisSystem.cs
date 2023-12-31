using System.Numerics;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Random;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Dataset;
using Content.Client.Chat.Managers;
using Content.Shared.Tag;
using System.Linq;
using Content.Shared.Imperial.ICCVar;
using Robust.Shared.Configuration;
using Content.Shared.Popups;
namespace Content.Client.Psychosis;

public sealed class PsychosisSystem : SharedPsychosisSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PsychosisComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<PsychosisComponent, PlayerDetachedEvent>(OnPlayerDetach);
        SubscribeNetworkEvent<PopUpTransfer>(PopupChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (_cfg.GetCVar(ICCVars.PsychosisEnabled) == true)
        {

            if (_player.LocalPlayer?.ControlledEntity is not EntityUid localPlayer)
                return;
            Checks(localPlayer);
        }
    }

    private void PopupChanged(PopUpTransfer psychosis, EntitySessionEventArgs args)
    {
        if (!TryComp<PsychosisComponent>(GetEntity(psychosis.Psychosis), out var psych))
            return;
        psych.PopUp = psychosis.Popup;
    }

    private void OnComponentStartup(EntityUid uid, PsychosisComponent component, ComponentStartup args)
    {
        if (_cfg.GetCVar(ICCVars.PsychosisEnabled) == false)
            return;
        var dataset = _prototypeManager.Index<DatasetPrototype>("PsychosisSoundTableStage1").Values;
        foreach (var thing in dataset)
        {
            component.SoundsTable.Add(new SoundPathSpecifier(thing));
        }
        var dataset2 = _prototypeManager.Index<DatasetPrototype>("PsychosisObjectTableStage1").Values;
        foreach (var thing in dataset2)
        {
            component.ItemsTable.Add(thing);
        }
        var dataset3 = _prototypeManager.Index<DatasetPrototype>("PsychosisCreature").Values;
        foreach (var thing in dataset3)
        {
            component.CreatureTable.Add(thing);
        }
        component.NextSoundTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(component.MinTimeBetweenSounds, component.MaxTimeBetweenSounds));
        var msg = new GetPopup(GetNetEntity(uid));
        RaiseNetworkEvent(msg);
    }

    private void OnPlayerDetach(EntityUid uid, PsychosisComponent component, PlayerDetachedEvent args)
    {
        if (_cfg.GetCVar(ICCVars.PsychosisEnabled) == false)
            return;
        component.Stream?.Stop();
    }
    private void StageChanged(EntityUid uid, PsychosisComponent psychosis)
    {
        if (_cfg.GetCVar(ICCVars.PsychosisEnabled) == false)
            return;
        if (psychosis.Stage == 3)
        {
            psychosis.MaxTimeBetweenSounds = 20f;
            psychosis.MinTimeBetweenSounds = 10f;
            psychosis.MaxTimeBetweenItems = 20f;
            psychosis.MinTimeBetweenItems = 10f;
        }
        if (psychosis.Stage == 2)
        {
            psychosis.MaxTimeBetweenSounds = 25f;
            psychosis.MinTimeBetweenSounds = 15f;
        }
        psychosis.Current = 0;
        psychosis.ItemsTable.Clear();
        var dataset22 = _prototypeManager.Index<DatasetPrototype>("PsychosisObjectTableStage" + psychosis.Stage.ToString()).Values;
        foreach (var thing in dataset22)
        {
            psychosis.ItemsTable.Add(thing);
        }
        psychosis.SoundsTable.Clear();
        var dataset2 = _prototypeManager.Index<DatasetPrototype>("PsychosisSoundTableStage" + psychosis.Stage.ToString()).Values;
        foreach (var thing in dataset2)
        {
            psychosis.SoundsTable.Add(new SoundPathSpecifier(thing));
        }
        var msg = new StageChange(psychosis.Stage, GetNetEntity(uid));
        RaiseNetworkEvent(msg);
    }
    private void PlaySounds(EntityUid uid, PsychosisComponent psychosis)
    {
        if (_cfg.GetCVar(ICCVars.PsychosisEnabled) == false)
            return;
        var timeInterval = _random.NextFloat(psychosis.MinTimeBetweenSounds, psychosis.MaxTimeBetweenSounds);
        psychosis.NextSoundTime = _timing.CurTime + TimeSpan.FromSeconds(timeInterval);
        psychosis.Sounds = _random.Pick(psychosis.SoundsTable);
        var randomOffset =
            new Vector2
            (
                _random.NextFloat(-psychosis.MaxDistance, psychosis.MaxDistance),
                _random.NextFloat(-psychosis.MaxDistance, psychosis.MaxDistance)
            );
        var newCoords = Transform(uid).Coordinates.Offset(randomOffset);

        // Play the sound
        psychosis.Stream = _audio.PlayStatic(psychosis.Sounds, uid, newCoords);
    }
    private void SpawnItem(EntityUid uid, PsychosisComponent psychosis)
    {
        if (psychosis.Stage >= 2)
        {
            var timeInterval = _random.NextFloat(psychosis.MinTimeBetweenItems, psychosis.MaxTimeBetweenItems);
            psychosis.NextItemTime = _timing.CurTime + TimeSpan.FromSeconds(timeInterval);
            var picked = true;
            var cords = Transform(uid).Coordinates;
            for (var spawnAttempt = 0; spawnAttempt < 5; spawnAttempt++)
            {
                var randomOffset =
                new Vector2
                (
                    _random.NextFloat(-psychosis.MaxItemDistance, psychosis.MaxItemDistance),
                    _random.NextFloat(-psychosis.MaxItemDistance, psychosis.MaxItemDistance)
                );
                var newCoords = Transform(uid).Coordinates.Offset(randomOffset);
                var entities = _entityLookupSystem.GetEntitiesInRange(newCoords, 0.5f, LookupFlags.All);
                foreach (var uidthing in entities)
                {
                    if (!TryComp<TagComponent>(uidthing, out var tagcomp))
                        continue;
                    foreach (var tag in tagcomp.Tags)
                    {
                        if (_prototypeManager.Index<DatasetPrototype>("PsychosisRestricted").Values.Contains(tag))
                        {
                            picked = false;
                        }
                    }
                }
                if (picked)
                {
                    cords = newCoords;
                    break;
                }
            }
            if (picked == true)
                if (_random.Prob(psychosis.ChanceForCreature))
                {
                    Spawn(_random.Pick(psychosis.CreatureTable), cords);
                }
                else
                {
                    Spawn(_random.Pick(psychosis.ItemsTable), cords);
                }
        }
    }
    public void Increase(EntityUid uid, PsychosisComponent psychosis, float increas)
    {
        var uid2 = _player.LocalPlayer?.ControlledEntity;
        if (uid2 == null)
            return;
        psychosis.Current += increas;
        psychosis.NextIncrease = _timing.CurTime + psychosis.IncreaseTime;
        EntityManager.EntitySysManager.GetEntitySystem<SharedPopupSystem>().PopupClient(Loc.GetString(psychosis.PopUp), uid, uid2.Value);
        if (psychosis.Current >= 100)
        {
            if (!(psychosis.Stage >= psychosis.Maxstage))
            {
                psychosis.Stage += 1;
                StageChanged(uid, psychosis);
            }
        }
    }
    private void Checks(EntityUid uid)
    {
        if (_cfg.GetCVar(ICCVars.PsychosisEnabled) == false)
            return;
        if (!TryComp<PsychosisComponent>(uid, out var psychosis))
            return;
        if (_timing.CurTime > psychosis.NextSoundTime)
            PlaySounds(uid, psychosis);
        if (_timing.CurTime > psychosis.NextItemTime)
            SpawnItem(uid, psychosis);
        if (_timing.CurTime > psychosis.NextIncrease)
            Increase(uid, psychosis, psychosis.Increase);
    }

}
//var ray = new CollisionRay(from.Position, dir, hitscan.CollisionMask);
//var rayCastResults =
//    Physics.IntersectRay(from.MapId, ray, hitscan.MaxLength, lastUser, false).ToList();
//if (!rayCastResults.Any())
