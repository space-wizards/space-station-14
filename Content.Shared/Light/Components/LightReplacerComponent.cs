using Content.Shared.Light.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Light.Components;

/// <summary>
/// Device that allows user to quickly change bulbs in <see cref="PoweredLightComponent"/>
/// Can be reloaded by new light tubes or light bulbs
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(LightReplacerSystem))]
public sealed partial class LightReplacerComponent : Component
{
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Weapons/click.ogg")
    {
        Params = new AudioParams
        {
            Volume = -4f,
        }
    };

    /// <summary>
    /// This string defines what kind of tube will be inserted into light fixtures.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId ActiveLightTube = "LightTube";

    /// <summary>
    /// This string defines what kind of bulb will be inserted into light fixtures.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId ActiveLightBulb = "LightBulb";
}
