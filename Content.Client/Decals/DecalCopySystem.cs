using Content.Client.Decals.Overlays;
using Content.Shared.Decals;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client.Decals;

public sealed class DecalCopySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly DecalPlacementSystem _decalPlacementSystem = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;

    public Action<Color> UpdateClientColorAction = default!;
    private bool _isActive = false;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder.Bind(EngineKeyFunctions.EditorPlaceObject, new PointerStateInputCmdHandler(
            // LMB
            (session, coords, uid) =>
            {
                if (!_isActive || !TryGetCurrentDecal(out var decal) || decal.Id == string.Empty)
                    return false;

                _decalPlacementSystem.UpdateDecalInfo(
                    id: decal.Id,
                    color: decal.Color != null ? decal.Color.Value : Color.White,
                    rotation: (float)decal.Angle.Degrees,
                    snap: _decalPlacementSystem.GetCurrentSnap(),
                    zIndex: decal.ZIndex,
                    cleanable: decal.Cleanable
                );

                if (decal.Color != null)
                    UpdateClientColorAction.Invoke(decal.Color.Value);

                return true;
            },
            (session, coords, uid) =>
            {
                if (!_isActive)
                    return false;

                SetActive(false);

                _decalPlacementSystem.SetActive(true);

                return true;
            }))
            // RMB
            .Bind(EngineKeyFunctions.EditorCancelPlace, command: new PointerStateInputCmdHandler(
            (session, coords, uid) =>
            {
                if (!_isActive)
                    return false;

                SetActive(false);

                _decalPlacementSystem.SetActive(true);

                return true;
            }, (session, coords, uid) =>
            {
                return true;
            }))
            // NUM9
            .Bind(ContentKeyFunctions.EditorNextObject, new PointerStateInputCmdHandler(
            (session, coords, uid) =>
            {
                if (!_isActive)
                    return false;

                ChooseOthetDecale(true);
                return true;
            }, (session, coords, uid) =>
            {
                if (!_isActive)
                    return false;

                return true;
            }))
            // NUM3
            .Bind(ContentKeyFunctions.EditorPreviousObject, new PointerStateInputCmdHandler(
            (session, coords, uid) =>
            {
                if (!_isActive)
                    return false;

                ChooseOthetDecale(false);
                return true;
            }, (session, coords, uid) =>
            {
                if (!_isActive)
                    return false;

                return true;
            })).Register<DecalCopySystem>();
    }

    public void SetActive(bool isActive)
    {
        _isActive = isActive;

        SwitchOverlay(_isActive);

        if (_isActive)
            _inputManager.Contexts.SetActiveContext("editor");
        else
            _inputSystem.SetEntityContextActive();
    }

    private void SwitchOverlay(bool isActive)
    {
        if (isActive == _overlayManager.HasOverlay<DecalCopyOverlay>())
            return;

        if (!isActive)
            _overlayManager.RemoveOverlay<DecalCopyOverlay>();
        else
            _overlayManager.AddOverlay(new DecalCopyOverlay());
    }

    private bool TryGetCurrentDecal(out Decal decal)
    {
        decal = new();

        if (!_isActive)
            return false;

        if (!_overlayManager.TryGetOverlay<DecalCopyOverlay>(out var overlay) || overlay == null)
            return false;

        decal = overlay.CurrentDecal;

        return true;
    }

    private void ChooseOthetDecale(bool isNext)
    {
        if (!_isActive)
            return;

        if (!_overlayManager.TryGetOverlay<DecalCopyOverlay>(out var overlay) || overlay == null)
            return;

        overlay.DecalIndex = isNext ? (short)(overlay.DecalIndex + 1) : (short)(overlay.DecalIndex - 1);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        SetActive(false);
        CommandBinds.Unregister<DecalCopySystem>();
    }
}
