using System.Numerics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client._Offbrand.BodyVisuals;

public sealed class OffbrandHealthDollControl : SpriteView
{
    private static readonly EntProtoId DollPrototype = "OffbrandHealthDoll";

    private EntityUid? _body;
    private readonly BodyAppearanceRelaySystem _relay;

    public OffbrandHealthDollControl()
    {
        _relay = EntMan.System<BodyAppearanceRelaySystem>();

        OverrideDirection = Direction.South;
        Scale = new Vector2(2, 2);
        SetSize = new Vector2(64, 64);
    }

    public void SetBody(EntityUid? body)
    {
        if (_body is { } oldBody && Entity is { } oldDoll)
            _relay.RemoveTarget(oldBody, oldDoll);

        _body = body;

        if (_body is { } newBody && Entity is { } doll)
            _relay.AddTarget(newBody, doll);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        if (Entity is not null)
            return;

        var doll = EntMan.SpawnEntity(DollPrototype, MapCoordinates.Nullspace);
        SetEntity(doll);

        if (_body is { } body)
            _relay.AddTarget(body, doll);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        if (Entity is { } doll)
            EntMan.DeleteEntity(doll);

        SetEntity(null);
    }
}
