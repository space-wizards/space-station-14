using Content.Shared.Damage.Components;

namespace Content.Server.Administration.Verbs.Operations;

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

public sealed partial class SetGodmodeOperation : AdminOperationBase<SetGodmodeOperation>
{
    [DataField(required: true)]
    public bool Enabled { get; private set; }
}
