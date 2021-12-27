using Content.Server.Materials;
using Content.Server.Mining.Components;
using Content.Server.Stack;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Mining.Systems;

public class ReprocessorSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ReprocessorComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, ReprocessorComponent component, InteractUsingEvent args)
    {
        if (!TryComp(args.Used, out MeltableComponent? melt))
            return;
        if (!TryComp(args.Used, out StackComponent? stack))
            return;

        var mat = _prototypeManager.Index<MaterialPrototype>(melt.Material);
        if (mat.StackId is null)
            return;

        _stackSystem.Spawn(
            stack.Count * melt.Units,
            _prototypeManager.Index<StackPrototype>(mat.StackId),
            Transform(uid).Coordinates
            );
        Del(uid);
        args.Handled = true;
    }
}
