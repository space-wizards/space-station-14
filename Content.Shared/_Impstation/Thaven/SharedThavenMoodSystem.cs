using Content.Shared.Emag.Systems;
using Content.Shared._Impstation.Thaven.Components;

namespace Content.Shared._Impstation.Thaven;

public abstract class SharedThavenMoodSystem : EntitySystem
{
    [Dependency] private readonly EmagSystem _emag = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThavenMoodsComponent, GotEmaggedEvent>(OnEmagged);
    }

    protected virtual void OnEmagged(EntityUid uid, ThavenMoodsComponent comp, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        if (uid == args.UserUid)
            return;

        args.Handled = true;
    }
}
