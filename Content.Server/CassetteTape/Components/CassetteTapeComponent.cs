using Content.Server.CassetteTape.EntitySystems;
using Content.Shared.CassetteTape;

namespace Content.Server.CassetteTape.Components;

/// <summary>
/// Simple magnetic-storage audio cassette tape.
/// </summary>
[RegisterComponent]
[Virtual]
public partial class CassetteTapeComponent : Component
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    /// <summary>
    /// Maximum recording time of the tape in seconds
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float LengthSeconds
    {
        get => _lengthSeconds;
        set => _entMan.System<CassetteTapeSystem>().SetTapeLength(Owner, value, this);
    }

    [DataField("lengthSeconds")]
    [Access(typeof(CassetteTapeSystem), typeof(CassettePlayheadSystem))]
    public float _lengthSeconds;

    /// <summary>
    /// The full stored audio on the tape.
    /// </summary>
    [DataField("storedAudioData")]
    [Access(typeof(CassetteTapeSystem), typeof(CassettePlayheadSystem))]
    public List<CassetteTapeAudioInfo> StoredAudioData { get; set; } = new();
}

