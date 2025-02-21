using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Shared.Rummaging;

/// <summary>
/// This is used for entities that can be
/// rummaged through to get loot.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RummagingSystem)), AutoGenerateComponentPause]
public sealed partial class RummageableComponent : Component
{
    /// <summary>
    /// Tracks whether or not this particular object has been looted.
    /// </summary>
    public bool Looted = false;

    /// <summary>
    /// Whether or not this entity can be rummaged through multiple times.
    /// </summary>
    [DataField]
    public bool Relootable = false;

    [DataField, AutoPausedField]
    public TimeSpan RelootableCooldown = TimeSpan.FromSeconds(60);
    public TimeSpan NextRelootable;

    /// <summary>
    /// A weighted loot table. 
    /// Overrides the same setting on RummagingComponent.
    /// </summary>
    [DataField]
    public EntityTableSelector? RummageLoot;

    /// <summary>
    /// How long it takes to rummage through a rummageable container.
    /// </summary>
    [DataField]
    public TimeSpan RummageDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Sound played on rummage completion.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundCollectionSpecifier("storageRustle");
}
