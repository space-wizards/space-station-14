using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Atmos.Piping.Binary.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Access;

public sealed class AgentIdCardSystem : SharedAgentIdCardSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IdCardComponent, AfterAutoHandleStateEvent>(OnIdState);
    }

    protected void UpdateUi(Entity<IdCardComponent> entity)
    {
        if (_ui.TryGetOpenUi(entity.Owner, AgentIDCardUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }

    private void OnIdState(Entity<IdCardComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }
}
