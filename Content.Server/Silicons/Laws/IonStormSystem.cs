using Content.Shared.Silicons.Laws.Components;

namespace Content.Server.Silicons.Laws;

public sealed partial class IonStormSystem : EntitySystem
{
    /// <summary>
    /// Triggers the ion storm event and subsequent handlers.
    /// Borg ion storm logic moved to <see cref="SiliconLawSystem"/>
    /// </summary>
    public void IonStormTarget(Entity<IonStormTargetComponent> ent, bool adminlog = true)
    {
        var ev = new IonStormEvent(adminlog);
        RaiseLocalEvent(ent, ref ev);
    }
}
