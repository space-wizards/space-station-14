using Content.Shared.Atmos.Piping.Portable.Components;
using Content.Shared.Atmos.Portable;

namespace Content.Client.Atmos.EntitySystems;

public sealed partial class SpaceHeaterSystem : SharedSpaceHeaterSystem
{

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<SpaceHeaterComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    protected override void UpdateUi(Entity<SpaceHeaterComponent> ent)
    {
        if (_ui.TryGetOpenUi(ent.Owner, SpaceHeaterUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}
