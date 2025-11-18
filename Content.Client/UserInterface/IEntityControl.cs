namespace Content.Client.UserInterface;

/// <summary>
/// Simple interface that indicates that the given control is associated with some entity.
/// This is primarily intended to be used with VV, so that you can easily open the VV window to examine an entity.
/// </summary>
public interface IEntityControl
{
    EntityUid? UiEntity { get; }
}
