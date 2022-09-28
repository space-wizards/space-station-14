using Content.Server.Humanoid.Components;
using Content.Server.RandomMetadata;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Humanoid.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class RandomHumanoidSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    [Dependency] private readonly HumanoidSystem _humanoid = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RandomHumanoidComponent, MapInitEvent>(OnMapInit,
            after: new []{ typeof(RandomMetadataSystem) });
    }

    private void OnMapInit(EntityUid uid, RandomHumanoidComponent component, MapInitEvent args)
    {
        QueueDel(uid);
        SpawnRandomHumanoid(component.RandomSettingsId, Transform(uid).Coordinates, MetaData(uid).EntityName);
    }

    public EntityUid SpawnRandomHumanoid(string prototypeId, EntityCoordinates coordinates, string name)
    {
        if (!_prototypeManager.TryIndex<RandomHumanoidPrototype>(prototypeId, out var prototype))
        {
            throw new ArgumentException("Could not get random humanoid settings");
        }

        var profile = HumanoidCharacterProfile.Random(prototype.SpeciesBlacklist);
        var speciesProto = _prototypeManager.Index<SpeciesPrototype>(profile.Species);
        var humanoid = Spawn(speciesProto.Prototype, coordinates);

        MetaData(humanoid).EntityName = prototype.RandomizeName
            ? profile.Name
            : name;

        _humanoid.LoadProfile(humanoid, profile);

        if (prototype.Components == null)
        {
            return humanoid;
        }

        foreach (var entry in prototype.Components.Values)
        {
            var comp = (Component) _serialization.Copy(entry.Component);
            comp.Owner = humanoid;
            EntityManager.AddComponent(humanoid, comp, true);
        }

        return humanoid;
    }
}
