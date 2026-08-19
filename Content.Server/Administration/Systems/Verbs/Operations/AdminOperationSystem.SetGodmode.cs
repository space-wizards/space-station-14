using Content.Shared.Administration.Verbs.Operations;
using Content.Shared.Damage.Components;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnSetGodmode(Entity<MetaDataComponent> entity, ref AdminOperationEvent<SetGodmodeOperation> args)
    {
        if (args.Operation.Enabled == HasComp<GodmodeComponent>(entity))
            return;

        if (args.Operation.Enabled)
            _godmode.EnableGodmode(entity);
        else
            _godmode.DisableGodmode(entity);
    }
}
