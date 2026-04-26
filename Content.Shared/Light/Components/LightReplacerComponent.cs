using Content.Shared.Light.EntitySystems;
using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

/// <summary>
///     Device that allows user to quickly change bulbs in <see cref="PoweredLightComponent"/>
///     Can be reloaded by new light tubes or light bulbs
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(LightReplacerSystem)), AutoGenerateComponentState]
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
    /// Bulbs that were inserted inside light replacer
    /// </summary>
    [ViewVariables]
    public Container InsertedBulbs = default!;

    /// <summary>
    /// This string defines what kind of tube will be inserted into light fixtures.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ActiveLightTube = "fluorescent light tube";

    /// <summary>
    /// This string defines what kind of bulb will be inserted into light fixtures.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ActiveLightBulb = "incandescent light bulb";

    /// <summary>
    /// The default starting bulbs
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> StartingContent = [];
}
