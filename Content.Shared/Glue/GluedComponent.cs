using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Humanoid;
using System.Xml.Linq;


namespace Content.Shared.Glue;

[RegisterComponent, NetworkedComponent]
public sealed class GluedComponent : Component
{
    [DataField("beforeGluedEntityName"), ViewVariables(VVAccess.ReadOnly)]
    public string BeforeGluedEntityName = String.Empty;

    [DataField("squeeze")]
    public SoundSpecifier Squeeze = new SoundPathSpecifier("/Audio/Items/squeezebottle.ogg");

    [DataField("glued")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Glued;

    [DataField("glueTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan GlueTime = TimeSpan.Zero;

    [DataField("glueCooldown")]
    public TimeSpan GlueCooldown = TimeSpan.FromSeconds(30);
}
