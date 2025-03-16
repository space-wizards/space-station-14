using Content.Server.DoAfter;
using Content.Shared.Flora;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Content.Server.Kitchen.Components;

namespace Content.Server.Flora;

public sealed partial class SharpenSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SharpenableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SharpenableComponent, SharpenDoAfterEvent>(OnDoAfter);
    }

    private void OnInteractUsing(EntityUid uid, SharpenableComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Verifica se a entidade usada tem o componente Sharp
        if (!HasComp<SharpComponent>(args.Used))
            return;

        // Inicia o DoAfter
        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.SharpenTime, new SharpenDoAfterEvent(), uid, used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, SharpenableComponent component, ref SharpenDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        // Cria o SharpenedStick na mesma posição
        var sharpenedStick = Spawn(component.ResultPrototype, Transform(uid).MapPosition);

        // Remove o LeafedStick original
        QueueDel(uid);

        args.Handled = true;
    }
}