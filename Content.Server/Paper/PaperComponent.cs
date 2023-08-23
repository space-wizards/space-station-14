using Content.Shared.Paper;
using Content.Shared.SS220.Photocopier;
using Robust.Shared.GameStates;

namespace Content.Server.Paper;

[NetworkedComponent, RegisterComponent]
public sealed partial class PaperComponent : SharedPaperComponent, IPhotocopyableComponent
{
    public PaperAction Mode;
    [DataField("content")]
    public string Content { get; set; } = "";

    /// <summary>
    ///     Allows to forbid to write on paper without using stamps as a hack
    /// </summary>
    [DataField("writable")]
    public bool Writable { get; set; } = true;

    [DataField("contentSize")]
    public int ContentSize { get; set; } = 6000;

    [DataField("stampedBy")]
    public List<StampDisplayInfo> StampedBy { get; set; } = new();

    /// <summary>
    ///     Stamp to be displayed on the paper, state from bureaucracy.rsi
    /// </summary>
    [DataField("stampState")]
    public string? StampState { get; set; }

    public IPhotocopiedComponentData GetPhotocopiedData()
    {
        return new PaperPhotocopiedData()
        {
            Content = Content,
            Writable = Writable,
            ContentSize = ContentSize,
            StampedBy = StampedBy,
            StampState = StampState
        };
    }
}

[Serializable]
public sealed class PaperPhotocopiedData : IPhotocopiedComponentData
{
    [Dependency, NonSerialized] private readonly IEntitySystemManager _sysMan = default!;

    public PaperPhotocopiedData()
    {
        IoCManager.InjectDependencies(this);
    }

    public string? Content;
    public bool? Writable;
    public int? ContentSize;
    public List<StampDisplayInfo>? StampedBy;
    public string? StampState;

    public void RestoreFromData(EntityUid uid, Component someComponent)
    {
        var paperSystem = _sysMan.GetEntitySystem<PaperSystem>();

        if (someComponent is not PaperComponent paperComponent)
            return;

        if (ContentSize is { } contentSize)
            paperComponent.ContentSize = contentSize;

        //Don't set empty content string so empty paper notice is properly displayed
        if (!string.IsNullOrEmpty(Content))
            paperSystem.SetContent(uid, Content, paperComponent);

        if (Writable is { } writable)
            paperComponent.Writable = writable;

        // Apply stamps
        if (StampState is null || StampedBy is null)
            return;

        foreach (var stampedBy in StampedBy)
        {
            paperSystem.TryStamp(uid, stampedBy, StampState);
        }
    }
}
