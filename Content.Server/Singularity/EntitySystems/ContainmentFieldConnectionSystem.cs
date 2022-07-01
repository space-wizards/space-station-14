using Content.Server.Singularity.Components;
using Linguini.Syntax.Ast;

namespace Content.Server.Singularity.EntitySystems;

public sealed class ContainmentFieldConnectionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContainmentFieldComponent, ContainmentFieldConnectEvent>(OnConnect);
    }

    private void OnConnect(EntityUid uid, ContainmentFieldComponent component, ContainmentFieldConnectEvent args)
    {
        
    }
}
