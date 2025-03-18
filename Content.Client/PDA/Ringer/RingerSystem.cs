using Content.Shared.PDA;
using Content.Shared.PDA.Ringer;

namespace Content.Client.PDA.Ringer;

public sealed class RingerSystem : SharedRingerSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RingerComponent, AfterAutoHandleStateEvent>(OnRingerUpdate);
    }

    private void OnRingerUpdate(Entity<RingerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateRingerUi(ent);
    }

    protected override void UpdateRingerUi(Entity<RingerComponent> ent)
    {
        if (UI.TryGetOpenUi(ent.Owner, RingerUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}
