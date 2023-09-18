namespace Content.Server.Ame.Components;

/// <summary></summary>
[RegisterComponent]
public sealed partial class AmeComponent : Component
{
    /// <summary></summary>
    [ViewVariables]
    public HashSet<EntityUid> Cores = new();

    /// <summary></summary>
    [ViewVariables]
    public EntityUid? MasterController = null;

    #region Timing

    /// <summary></summary>
    [ViewVariables]
    public TimeSpan LastUpdate = default!;

    /// <summary></summary>
    [ViewVariables]
    public TimeSpan NextUpdate = default!;

    /// <summary></summary>
    [DataField("updatePeriod")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan UpdatePeriod = TimeSpan.FromSeconds(10f);

    #endregion Timing
}
