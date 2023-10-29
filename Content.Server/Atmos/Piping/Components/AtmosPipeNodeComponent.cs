using Content.Server.Atmos.Piping.EntitySystems;
using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Components;

/// <summary>
/// 
/// </summary>
[Access(typeof(AtmosPipeNetSystem))]
[RegisterComponent]
public sealed partial class AtmosPipeNodeComponent : Component, IGasMixtureHolder
{
    /// <summary>The default volume of the gas mixture inside of this pipe.</summary>
    [ViewVariables]
    public const float DefaultVolume = 200f;

    /// <summary>The volume of the gas mixture inside of this pipe.</summary>
    [DataField("volume")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Volume = DefaultVolume;

    /// <summary>
    /// TODO: YEET THIS
    /// </summary>
    public GasMixture Air
    {
        get => IoCManager.Resolve<IEntityManager>().System<AtmosPipeNetSystem>().TryGetGas((Owner, this, null), out var gas) ? gas : new();
        set
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            if (!entMan.TryGetComponent<AtmosPipeNetComponent>(Owner, out var net))
                return;

            net.Air = value;
        }
    }
}
