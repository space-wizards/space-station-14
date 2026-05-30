using Content.Shared.Interaction;
using Content.Shared.Tools.Components;

namespace Content.Shared.Tools.Systems;

/// <summary>
/// This simple system implements the behavior of <see cref="SimpleToolInteractionComponent"/>, which is translating
/// <see cref="InteractUsingEvent"/>s with the correct entities into <see cref="ToolInteractionEvent"/>s."/>
/// </summary>
public sealed partial class SimpleToolInteractionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SimpleToolInteractionComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<SimpleToolInteractionComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled ||
            !TryComp<ToolComponent>(args.Used, out var toolComp))
            return;

        args.Handled = true;
        var ev = new ToolInteractionEvent((args.Used, toolComp), args.User, args.ClickLocation);
        RaiseLocalEvent(ent, ref ev);
    }
}
