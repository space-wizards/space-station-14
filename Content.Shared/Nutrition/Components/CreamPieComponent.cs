using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Component used for banana cream pies.
/// These can be thrown at someone to stun them and cream their face.
/// </summary>
[Access(typeof(SharedCreamPieSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CreamPieComponent : Component
{
    /// <summary>
    /// The time being hit by this entity will stun you.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ParalyzeTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The sound to play when hitting something.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("desecration", AudioParams.Default.WithVariation(0.125f));

    /// <summary>
    /// Has this pie been splatted by hitting something?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Splatted = false;

    /// <summary>
    /// Items in this container will be triggered when the pie hits something.
    /// This allows throwable C4 pies or similar.
    /// </summary>
    [ViewVariables]
    public const string PayloadSlotName = "payloadSlot";
}
