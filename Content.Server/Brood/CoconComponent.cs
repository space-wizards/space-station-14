

using Content.Server.Stunnable;

[RegisterComponent, Access(typeof(WebSpitStunSystem))]
internal sealed class CoconComponent : Component
{


    [DataField("paralyzeTime"), ViewVariables(VVAccess.ReadWrite)]
    public float ParalyzeTime = 5f;

    [DataField("equipedOn"), ViewVariables(VVAccess.ReadWrite)]
    public EntityUid EquipedOn;

    [ViewVariables] public float Accumulator = 0;

    [DataField("damageFrequency"), ViewVariables(VVAccess.ReadWrite)]
    public float DamageFrequency = 0.1f;


}

