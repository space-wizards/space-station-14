using Content.Shared.Actions;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Magic.Events;

public class SpawnSpellEvent : WorldTargetActionEvent
{
    /// <summary>
    /// The prototype that the spell is going to spawn
    /// </summary>
    //[DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    //public string Prototype = "";

    [DataField("prototypes")]
    public List<EntitySpawnEntry> Contents = new();

    [DataField("offsetVector2")]
    public Vector2 OffsetVector2;

    [DataField("temporarySummon")]
    public bool TemporarySummon = false;
}

