using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Access;

/// <inheritdoc />
public sealed class AgentIdCardSystem : SharedAgentIdCardSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        // Not AgentIDCardComponent as the IdCardComponent is the one with the changing state
        SubscribeLocalEvent<IdCardComponent, AfterAutoHandleStateEvent>(OnIdState);
    }

    protected override void UpdateUi(EntityUid entity)
    {
        if (_ui.TryGetOpenUi(entity, AgentIDCardUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }

    private void OnIdState(Entity<IdCardComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }
}
