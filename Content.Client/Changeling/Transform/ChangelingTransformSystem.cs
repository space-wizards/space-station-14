using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Changeling.Transform;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.Changeling.Transform;
public sealed class ChangelingTransformSystem : SharedChangelingTransformSystem
{
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;


    private ChangelingTransformMenu? _menu;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ChangelingIdentityResponse>(OnBuildResponse);
    }

    public void StoreMenu(ChangelingTransformMenu menu)
    {
        _menu = menu;
    }

    private void OnBuildResponse(ChangelingIdentityResponse response)
    {
        var _player = _playerManager.LocalEntity.GetValueOrDefault();

        if (!_entManager.EntityExists(_player))
            return;
        if (!TryComp<ChangelingTransformComponent>(_player, out var comp) && comp?.ChangelingTransformActionEntity != null)
            return;
        if(!TryComp<UserInterfaceComponent>(comp?.ChangelingTransformActionEntity, out var uiEntity))
            return;
        _userInterface.TryGetOpenUi<ChangelingTransformBoundUserInterface>(uiEntity.Owner, TransformUi.Key, out var ui);
        var a = "";
    }
}

