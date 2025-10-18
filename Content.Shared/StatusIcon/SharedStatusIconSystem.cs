namespace Content.Shared.StatusIcon;

public abstract class SharedStatusIconSystem : EntitySystem
{
    // If you are trying to add logic for status icons here, you're probably in the wrong place.
    // Status icons are gathered and rendered entirely clientside.
    // If you wish to use data to render icons, you should replicate that data to the client
    // and subscribe to GetStatusIconsEvent in order to add the relevant icon to a given entity.
}
