using Content.Shared.Mesons;
using Content.Shared.Toggleable;

namespace Content.Server.Mesons;

public sealed class MesonsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MesonsComponent, ToggleActionEvent>(Toggle);
    }

    private void Toggle(Entity<MesonsComponent> uid, ref ToggleActionEvent args)
    {
        if (uid.Comp.Enabled)
            Disable(uid);
        else
            Enable(uid);
        Dirty(uid);
    }

    public void Disable(Entity<MesonsComponent> uid)
    {
        uid.Comp.Enabled = false;
        _appearance.SetData(uid, ToggleVisuals.Toggled, false);
    }

    public void Enable(Entity<MesonsComponent> uid)
    {
        uid.Comp.Enabled = true;
        _appearance.SetData(uid, ToggleVisuals.Toggled, true);
    }
}
