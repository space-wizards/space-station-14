using Content.Shared.Decals;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;

namespace Content.Client.Decals;

public sealed class DecalPlacementSystem : EntitySystem
{
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private InputSystem _inputSystem = default!;

    private string _decalId = "0";
    private Color _decalColor = Color.White;
    private Angle _decalAngle = Angle.Zero;
    private bool _snap = false;

    private bool _active = false;
    private bool _placing = false;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder.Bind(ContentKeyFunctions.PlaceDecal, new PointerStateInputCmdHandler(
            ((session, coords, uid) =>
            {
                if (!_active)
                    return false;

                if (_placing)
                    return false;
                _placing = true;

                coords = _snap ? coords.AlignWithClosestGridTile() : coords;
                var decal = new Decal(coords.Position, _decalId, _decalColor, _decalAngle, 0, false);
                RaiseNetworkEvent(new RequestDecalPlacementEvent(decal, coords.GetGridId(EntityManager)));

                return true;
            }),
            ((session, coords, uid) =>
            {
                if (!_active)
                    return false;

                _placing = false;
                return true;
            }), true)).Register<DecalPlacementSystem>();
    }

    public void UpdateDecalInfo(string id, Color color, float rotation, bool snap)
    {
        _decalId = id;
        _decalColor = color;
        _decalAngle = Angle.FromDegrees(rotation);
        _snap = snap;
    }

    public void SetActive(bool active)
    {
        _active = active;
        //if (_active)
        //    _inputManager.Contexts.SetActiveContext("editor");
        //else
        //    _inputSystem.SetEntityContextActive();
    }
}
