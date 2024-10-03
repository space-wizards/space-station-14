using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Impstation.Spelfs.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpelfMoodComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool FollowsSharedMoods = true;

    [DataField, ViewVariables]
    public List<SpelfMood> Moods = new();

    [DataField(customTypeSerializer: typeof(PrototypeIdHashSetSerializer<SpelfMoodPrototype>)), ViewVariables]
    public HashSet<string> Conflicts = new();

    public HashSet<string> MoodProtoSet()
    {
        var moodProtos = new HashSet<string>();
        foreach (var mood in Moods)
            if (!string.IsNullOrEmpty(mood.ProtoId))
                moodProtos.Add(mood.ProtoId);
        return moodProtos;
    }
}
