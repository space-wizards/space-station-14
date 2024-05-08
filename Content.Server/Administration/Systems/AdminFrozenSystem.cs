using Content.Server.Speech.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Speech;

namespace Content.Server.Administration.Systems;

public sealed class AdminFrozenSystem : SharedAdminFrozenSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AdminFrozenComponent, ScreamActionEvent>(OnScreamAction, before: new[] { typeof(VocalSystem) });
    }

    private void OnScreamAction(EntityUid uid, AdminFrozenComponent component, ScreamActionEvent args)
    {
        if (!component.Muted)
            return;

        args.Handled = true;
    }

    /// <summary>
    /// Freezes and mutes the given entity.
    /// </summary>
    public void FreezeAndMute(EntityUid uid)
    {
        var comp = EnsureComp<AdminFrozenComponent>(uid);
        comp.Muted = true;
        Dirty(uid, comp);
    }
}
