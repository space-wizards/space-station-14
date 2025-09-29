using Content.Shared.Buckle.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Network;

namespace Content.Shared._Starlight.Railroading;

public sealed partial class ActivateUiOnStrappedSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly INetManager _net = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActivateUiOnStrappedComponent, StrappedEvent>(OnStrapped);
    }
    private void OnStrapped(EntityUid uid, ActivateUiOnStrappedComponent component, StrappedEvent args)
    {
        if (!_net.IsServer) return;

        var oae = new ActivatableUIOpenAttemptEvent(args.Buckle);
        var uae = new UserOpenActivatableUIAttemptEvent(args.Buckle, uid);
        RaiseLocalEvent(args.Buckle, uae);
        RaiseLocalEvent(uid, oae);

        if( oae.Cancelled || uae.Cancelled)
            return;

        var bae = new BeforeActivatableUIOpenEvent(args.Buckle);
        RaiseLocalEvent(uid, bae);

        _ui.OpenUi(uid, component.Key, args.Buckle);

        //Let the component know a user opened it so it can do whatever it needs to do
        var aae = new AfterActivatableUIOpenEvent(args.Buckle, args.Buckle);
        RaiseLocalEvent(uid, aae);
    }
}
