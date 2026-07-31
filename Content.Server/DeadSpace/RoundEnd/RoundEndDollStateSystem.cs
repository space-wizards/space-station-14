// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.GameTicking.Events;
using Content.Shared.Body.Components;
using Content.Shared.DeadSpace.RoundEnd;
using Content.Shared.GameTicking;
using Content.Shared.Gibbing;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.RoundEnd;

/// <summary>
/// Maintains compact round-end doll descriptions without spawning display entities or doing per-tick work.
/// </summary>
public sealed class RoundEndDollStateSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly RoundEndManifestStatsSystem _manifest = default!;

    private readonly Dictionary<EntityUid, DollState> _stateByMind = new();
    private readonly Dictionary<EntityUid, EntityUid> _mindByBody = new();
    private readonly Dictionary<EntityUid, EntityUid> _bodyByMind = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(_ => Reset());
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => Reset());
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<MindComponent, MindGotAddedEvent>(OnMindAdded);
        SubscribeLocalEvent<MindContainerComponent, BeforeMindRemovedMessage>(OnBeforeMindRemoved);
        SubscribeLocalEvent<MindContainerComponent, BeingGibbedEvent>(OnBeingGibbed);
        SubscribeLocalEvent<MindContainerComponent, DidEquipEvent>(OnEquipped);
        SubscribeLocalEvent<MindContainerComponent, DidUnequipEvent>(OnUnequipped);
        SubscribeLocalEvent<BodyComponent, EntityTerminatingEvent>(OnEntityTerminating);
    }

    public RoundEndDollData? GetDollData(EntityUid mindId)
    {
        if (!_stateByMind.TryGetValue(mindId, out var state))
            return null;

        return new RoundEndDollData
        {
            BodyPrototype = state.BodyPrototype,
            FallbackGear = state.FallbackGear,
            Humanoid = CloneAppearance(state.Humanoid),
            Equipment = state.Equipment
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new RoundEndDollEquipment
                {
                    Slot = pair.Key,
                    Prototype = pair.Value,
                })
                .ToArray(),
        };
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!TryComp<MindContainerComponent>(args.Mob, out var container) ||
            container.Mind is not { } mindId ||
            !TryComp<MindComponent>(mindId, out var mind))
        {
            return;
        }

        ProtoId<StartingGearPrototype>? fallbackGear = null;
        if (args.JobId != null &&
            _prototypes.TryIndex<JobPrototype>(args.JobId, out var job))
        {
            fallbackGear = job.StartingGear;
        }

        TryTrackBody(mindId, mind, args.Mob, fallbackGear);
    }

    private void OnMindAdded(EntityUid mindId, MindComponent mind, MindGotAddedEvent args)
    {
        TryTrackBody(mindId, mind, args.Container.Owner, null);
    }

    private void OnBeforeMindRemoved(
        EntityUid uid,
        MindContainerComponent component,
        BeforeMindRemovedMessage args)
    {
        if (_mindByBody.TryGetValue(uid, out var mindId) && mindId == args.Mind.Owner)
            CaptureBody(mindId, uid);
    }

    private void OnBeingGibbed(EntityUid uid, MindContainerComponent component, ref BeingGibbedEvent args)
    {
        if (!_mindByBody.Remove(uid, out var mindId))
            return;

        // Gibbing drops inventory before the body is deleted. Freeze the pre-gib state so those
        // DidUnequip events and EntityTerminating cannot replace it with an empty inventory.
        CaptureBody(mindId, uid);
        if (_bodyByMind.GetValueOrDefault(mindId) == uid)
            _bodyByMind.Remove(mindId);
    }

    private void OnEntityTerminating(EntityUid uid, BodyComponent component, ref EntityTerminatingEvent args)
    {
        if (!_mindByBody.Remove(uid, out var mindId))
            return;

        CaptureBody(mindId, uid);
        if (_bodyByMind.GetValueOrDefault(mindId) == uid)
            _bodyByMind.Remove(mindId);
    }

    private void OnEquipped(EntityUid uid, MindContainerComponent component, DidEquipEvent args)
    {
        if (!_mindByBody.TryGetValue(uid, out var mindId) ||
            !_stateByMind.TryGetValue(mindId, out var state) ||
            MetaData(args.Equipment).EntityPrototype?.ID is not { } prototype)
        {
            return;
        }

        state.Equipment[args.Slot] = prototype;
    }

    private void OnUnequipped(EntityUid uid, MindContainerComponent component, DidUnequipEvent args)
    {
        if (_mindByBody.TryGetValue(uid, out var mindId) &&
            _stateByMind.TryGetValue(mindId, out var state))
        {
            state.Equipment.Remove(args.Slot);
        }
    }

    private void TryTrackBody(
        EntityUid mindId,
        MindComponent mind,
        EntityUid body,
        ProtoId<StartingGearPrototype>? fallbackGear)
    {
        if (TerminatingOrDeleted(body) ||
            !HasComp<BodyComponent>(body) ||
            !_manifest.IsManifestCharacter(mindId, mind, body))
        {
            return;
        }

        if (_bodyByMind.TryGetValue(mindId, out var previousBody))
        {
            if (previousBody == body && _stateByMind.TryGetValue(mindId, out var existing))
            {
                if (_mindByBody.TryGetValue(body, out var previousMind) &&
                    previousMind != mindId &&
                    _bodyByMind.GetValueOrDefault(previousMind) == body)
                {
                    _bodyByMind.Remove(previousMind);
                }

                _mindByBody[body] = mindId;

                if (fallbackGear != null)
                    existing.FallbackGear = fallbackGear;

                return;
            }

            if (_mindByBody.GetValueOrDefault(previousBody) == mindId)
                _mindByBody.Remove(previousBody);
        }

        if (_mindByBody.TryGetValue(body, out var oldMind) &&
            oldMind != mindId &&
            _bodyByMind.GetValueOrDefault(oldMind) == body)
        {
            _bodyByMind.Remove(oldMind);
        }

        _bodyByMind[mindId] = body;
        _mindByBody[body] = mindId;

        CaptureBody(mindId, body);
        if (fallbackGear != null && _stateByMind.TryGetValue(mindId, out var state))
            state.FallbackGear = fallbackGear;
    }

    private void CaptureBody(EntityUid mindId, EntityUid body)
    {
        if (Deleted(body))
            return;

        var previousGear = _stateByMind.TryGetValue(mindId, out var previous)
            ? previous.FallbackGear
            : null;
        var state = new DollState
        {
            BodyPrototype = MetaData(body).EntityPrototype?.ID,
            FallbackGear = previousGear,
        };

        if (TryComp<HumanoidAppearanceComponent>(body, out var humanoid))
        {
            state.Humanoid = new RoundEndHumanoidAppearance
            {
                Species = humanoid.Species,
                Markings = new MarkingSet(humanoid.MarkingSet),
                PermanentlyHidden = new HashSet<HumanoidVisualLayers>(humanoid.PermanentlyHidden),
                CustomBaseLayers = new Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo>(humanoid.CustomBaseLayers),
                Gender = humanoid.Gender,
                Age = humanoid.Age,
                SkinColor = humanoid.SkinColor,
                Sex = humanoid.Sex,
                EyeColor = humanoid.EyeColor,
                HairGradientEnabled = humanoid.HairGradientEnabled,
                HairGradientColor = humanoid.HairGradientColor,
            };
        }

        if (TryComp<InventoryComponent>(body, out var inventory))
        {
            var slots = _inventory.GetSlotEnumerator((body, inventory));
            while (slots.NextItem(out var item, out var slot))
            {
                if (MetaData(item).EntityPrototype?.ID is { } prototype)
                    state.Equipment[slot.Name] = prototype;
            }
        }

        _stateByMind[mindId] = state;
    }

    private static RoundEndHumanoidAppearance? CloneAppearance(RoundEndHumanoidAppearance? appearance)
    {
        if (appearance == null)
            return null;

        return new RoundEndHumanoidAppearance
        {
            Species = appearance.Species,
            Markings = new MarkingSet(appearance.Markings),
            PermanentlyHidden = new HashSet<HumanoidVisualLayers>(appearance.PermanentlyHidden),
            CustomBaseLayers = new Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo>(appearance.CustomBaseLayers),
            Gender = appearance.Gender,
            Age = appearance.Age,
            SkinColor = appearance.SkinColor,
            Sex = appearance.Sex,
            EyeColor = appearance.EyeColor,
            HairGradientEnabled = appearance.HairGradientEnabled,
            HairGradientColor = appearance.HairGradientColor,
        };
    }

    private void Reset()
    {
        _stateByMind.Clear();
        _mindByBody.Clear();
        _bodyByMind.Clear();
    }

    private sealed class DollState
    {
        public EntProtoId? BodyPrototype;
        public ProtoId<StartingGearPrototype>? FallbackGear;
        public RoundEndHumanoidAppearance? Humanoid;
        public readonly Dictionary<string, EntProtoId> Equipment = new(StringComparer.Ordinal);
    }
}
