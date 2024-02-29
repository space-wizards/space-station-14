using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(ChangelingRuleSystem))]
public sealed partial class ChangelingRuleComponent : Component
{
    public readonly List<EntityUid> ChangelingMinds = new();

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string ChangelingPrototypeId = "Changeling";

    public int TotalChangelings => ChangelingMinds.Count;

    public Dictionary<ICommonSession, HumanoidCharacterProfile> StartCandidates = new();

    /// <summary>
    /// Path to changeling start sound.
    /// </summary>
    [DataField]
    public SoundSpecifier ChangelingStartSound = new SoundPathSpecifier("/Audio/Ambience/Antag/changeling_start.ogg");
}
