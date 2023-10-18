using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.DarkReaper;

[RegisterComponent, NetworkedComponent]
[Access(typeof(DarkReaperRuneSharedSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed partial class DarkReaperRuneComponent : Component
{
    [ViewVariables, DataField]
    public EntProtoId DarkReaperPrototypeId = "MobDarkReaper";

    [ViewVariables, DataField]
    public SoundSpecifier SpawnSound = new SoundPathSpecifier("/Audio/SS220/DarkReaper/jnec_start.ogg", new()
    {
        MaxDistance = 8
    });

    [DataField]
    public EntProtoId SpawnAction = "ActionDarkReaperSpawn";

    [ViewVariables]
    public EntityUid? SpawnActionEntity;
}
