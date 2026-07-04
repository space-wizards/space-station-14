using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;

namespace Content.Shared.NightVision;

/// <summary>
/// Shows/hides the <see cref="NightVisionOverlay"/> based on whether the observed
/// entity has a <see cref="NightVisionComponent"/> equipped.
/// </summary>
public abstract partial class SharedNightVisionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        // SubscribeLocalEvent<NightVisionComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    /// <summary>
    /// Enables or disables the component.
    /// </summary>
    public void SetEnabled(Entity<NightVisionComponent?> ent, bool enabled)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Enabled = enabled;

        // RefreshOverlay();
    }
}
