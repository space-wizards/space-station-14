using Content.Shared._FinalStand.Economy;
using Content.Shared._FinalStand.Shop;
using Robust.Client;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._FinalStand.Shop;

public sealed class FSShopClientSystem : EntitySystem
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private static readonly ProtoId<ShaderPrototype> ShaderAffordable = "FSShopGlowAffordable";
    private static readonly ProtoId<ShaderPrototype> ShaderUnaffordable = "FSShopGlowUnaffordable";

    public int CurrentCredits { get; private set; }
    public Dictionary<string, int> UpgradeLevels { get; private set; } = [];

    public event Action? CreditsChanged;
    public event Action? UpgradeLevelsChanged;

    private readonly Dictionary<EntityUid, bool> _lastAffordability = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<WalletUpdatedEvent>(OnWalletUpdate);
        SubscribeNetworkEvent<UpgradeLevelsUpdatedEvent>(OnUpgradesUpdated);
        _client.PlayerJoinedServer += OnJoined;
        _client.PlayerLeaveServer += OnLeft;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _client.PlayerJoinedServer -= OnJoined;
        _client.PlayerLeaveServer -= OnLeft;
        ClearAllShaders();
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<FSShopWeaponComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var shop, out var sprite))
        {
            var canAfford = CurrentCredits >= shop.Price;
            if (_lastAffordability.TryGetValue(uid, out var last) && last == canAfford)
                continue;

            _lastAffordability[uid] = canAfford;
            ApplyOutline(sprite, canAfford);
        }
    }

    private void OnWalletUpdate(WalletUpdatedEvent ev)
    {
        CurrentCredits = ev.Credits;
        _lastAffordability.Clear();
        CreditsChanged?.Invoke();
    }

    private void OnUpgradesUpdated(UpgradeLevelsUpdatedEvent ev)
    {
        UpgradeLevels = ev.Levels;
        UpgradeLevelsChanged?.Invoke();
    }

    private void OnJoined(object? _, PlayerEventArgs __)
    {
        _lastAffordability.Clear();
        UpgradeLevels = [];
    }

    private void OnLeft(object? _, PlayerEventArgs __)
    {
        ClearAllShaders();
        UpgradeLevels = [];
    }

    private void ApplyOutline(SpriteComponent sprite, bool canAfford)
    {
        var protoId = canAfford ? ShaderAffordable : ShaderUnaffordable;
        sprite.PostShader = _prototypeManager.Index(protoId).InstanceUnique();
    }

    private void ClearAllShaders()
    {
        var query = EntityQueryEnumerator<FSShopWeaponComponent, SpriteComponent>();
        while (query.MoveNext(out _, out _, out var sprite))
        {
            sprite.PostShader = null;
        }
        _lastAffordability.Clear();
    }
}
