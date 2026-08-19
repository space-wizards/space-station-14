using Content.Shared.Access.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Access;

/// <inheritdoc />
public sealed partial class AgentIdCardSystem : SharedAgentIdCardSystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void UpdateUi(EntityUid entity)
    {
        if (_ui.TryGetOpenUi(entity, AgentIDCardUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}
