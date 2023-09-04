using Content.Server.Light.EntitySystems;
using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Light.Components;

/// <summary>
///     Device that allows user to quikly change bulbs in <see cref="PoweredLightComponent"/>
///     Can be reloaded by new light tubes or light bulbs
/// </summary>
[RegisterComponent, Access(typeof(LightReplacerSystem))]
public sealed partial class LightReplacerComponent : Component
{
    [DataField("sound")]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Weapons/click.ogg")
    {
        Params = new()
        {
            Volume = -4f
        }
    };

    /// <summary>
    /// Bulbs that were inserted inside light replacer
    /// </summary>
    [ViewVariables]
    public Container InsertedBulbs = default!;

    /// <summary>
    /// The default starting bulbs
    /// </summary>
    [DataField("contents")]
    public List<EntitySpawnEntry> Contents = new();
}
