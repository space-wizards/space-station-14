using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// Means this entity is bound to silicon laws and can view them.
/// Requires to be linked with <see cref="SiliconLawProviderComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedSiliconLawSystem))]
public sealed partial class SiliconLawBoundComponent : Component
{
    /// <summary>
    /// Whether the LawsetProvider should be fetched on init.
    /// This also caches it's lawset in this component.
    /// </summary>
    [DataField]
    public bool FetchOnInit = true;

    /// <summary>
    /// Lawset created from the prototype id.
    /// Cached from the linked <see cref="SiliconLawProviderComponent"/> for the sake of prediction.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SiliconLawset Lawset = new ();

    /// <summary>
    /// The entity that currently provides laws to this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? LawsetProvider;

    /// <summary>
    /// Whether this provider is currently subverted.
    /// Cached from the linked <see cref="SiliconLawProviderComponent"/> for the sake of prediction.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Subverted = false;

    // Prevent cheat clients from seeing the laws of other players.
    public override bool SendOnlyToOwner => true;
}
