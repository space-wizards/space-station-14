using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Chasm.Components;

/// <summary>
///     Marks a component that will cause entities to fall into them on a step trigger activation
/// </summary>
[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(fieldDeltas:true), Access(typeof(ChasmSystem))]
public sealed partial class ChasmComponent : Component
{
    /// <summary>
    ///     Sound that should be played when an entity falls into the chasm
    /// </summary>
    [DataField("fallingSound")]
    public SoundSpecifier FallingSound = new SoundPathSpecifier("/Audio/Effects/falling.ogg");

    /// <summary>
    /// A list of entities that are currently falling into the chasm.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> FallingEntities = new();
}
