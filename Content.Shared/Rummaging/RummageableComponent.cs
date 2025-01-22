using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Rummaging;

/// <summary>
/// This is used for entities that can be
/// rummaged through to get loot.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RummagingSystem))]
[AutoGenerateComponentState]
public sealed partial class RummageableComponent : Component
{
    /// <summary>
    /// Whether or not this entity has been rummaged through already.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Looted;

    /// <summary>
    /// Whether or not this entity can be rummaged through multiple times.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Relootable = false;

    [DataField, AutoNetworkedField]
    public TimeSpan RelootableCooldown = TimeSpan.FromSeconds(60);
    public TimeSpan NextRelootable;

    /// <summary>
    /// A weighted loot table. 
    /// Overrides the same setting on RummagingComponent.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityTableSelector? RummageLoot;

    /// <summary>
    /// How long it takes to rummage through a rummageable container.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RummageDuration = 3f;

    /// <summary>
    /// Sound played on rummage completion.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundCollectionSpecifier("storageRustle");
}
