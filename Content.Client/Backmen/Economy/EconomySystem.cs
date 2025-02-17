// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Client.Backmen.CartridgeLoader.Cartridges;
using Content.Client.UserInterface.Fragments;
using Content.Shared.Backmen.Economy;
using Content.Shared.PDA;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client.Backmen.Economy;

public sealed class EconomySystem : EntitySystem
{
    [Dependency] private TransformSystem _transformSystem = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BankAccountComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void UpdatePdaUi(EntityUid uid, BankAccountComponent component)
    {
        foreach (var bankUi in EntityQuery<UIFragmentComponent>().Where(x=>x.Ui is BankUi))
        {
            var ui = bankUi.Ui as BankUi;
            ui?.Fragment?.FillFields();
        }
    }

    private void OnHandleState(EntityUid uid, BankAccountComponent component, ref AfterAutoHandleStateEvent args)
    {
        var parent = _transformSystem.GetParentUid(uid);
        if (!parent.IsValid())
        {
            return;
        }
        if (!HasComp<PdaComponent>(parent))
        {
            return;
        }
        var parentPlayer = _transformSystem.GetParentUid(parent);
        if (!parentPlayer.IsValid())
        {
            return;
        }
        if (_playerManager.LocalPlayer?.ControlledEntity != parent)
        {
            return;
        }
        UpdatePdaUi(uid, component);
    }
}
