using Content.Shared.Job;
using Robust.Shared.Prototypes;

namespace Content.Server.Job;

public sealed partial class RemoveComponentSpecial : JobSpecial
{
    [DataField(required: true)]
    public ComponentRegistry Components { get; private set; } = new();

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        entMan.RemoveComponents(mob, Components);
    }
}
