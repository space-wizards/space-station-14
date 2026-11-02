using Content.Shared.Atmos.Portable;
using Content.Shared.Atmos.Portable.Components;

namespace Content.Client.Atmos.EntitySystems;
/// <summary>
/// /////TODO WAAAAAH TOGGLE OFF IS NOT PREDICTED NOW
/// </summary>
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
