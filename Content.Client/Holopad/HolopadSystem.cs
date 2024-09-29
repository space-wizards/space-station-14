using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Holopad;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System;
using System.Linq;

namespace Content.Client.Holopad;

public sealed class HolopadSystem : SharedHolopadSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolopadHologramComponent, BeforePostShaderRenderEvent>(OnShaderRender);
        SubscribeAllEvent<TypingChangedEvent>(OnTypingChanged);

        SubscribeNetworkEvent<PlayerSpriteStateRequest>(OnPlayerSpriteStateRequest);
        SubscribeNetworkEvent<PlayerSpriteStateMessage>(OnPlayerSpriteStateMessage);
    }

    private void OnShaderRender(EntityUid uid, HolopadHologramComponent component, BeforePostShaderRenderEvent ev)
    {
        if (ev.Sprite.PostShader == null)
            return;

        ev.Sprite.PostShader.SetParameter("t", (float)_timing.RealTime.TotalSeconds * component.ScrollRate);
    }

    private void OnTypingChanged(TypingChangedEvent ev, EntitySessionEventArgs args)
    {
        var uid = args.SenderSession.AttachedEntity;

        if (!Exists(uid))
            return;

        if (!HasComp<HolopadUserComponent>(uid))
            return;

        var netEv = new HolopadUserTypingChangedEvent(GetNetEntity(uid.Value), ev.IsTyping);
        RaiseNetworkEvent(netEv);
    }

    private void OnPlayerSpriteStateRequest(PlayerSpriteStateRequest ev)
    {
        var targetPlayer = GetEntity(ev.TargetPlayer);
        var player = _playerManager.LocalSession?.AttachedEntity;

        if (targetPlayer != player)
            return;

        if (!TryComp<SpriteComponent>(player, out var playerSprite))
            return;

        var spriteLayerData = new List<SpriteLayerDatum>();

        if (playerSprite.Visible)
        {
            for (int i = 0; i < playerSprite.AllLayers.Count(); i++)
            {
                if (!playerSprite.TryGetLayer(i, out var layer))
                    continue;

                if (!layer.Visible ||
                    string.IsNullOrEmpty(layer.RSI?.Path.ToString()) ||
                    string.IsNullOrEmpty(layer.State.Name))
                    continue;

                var layerDatum = new SpriteLayerDatum(layer.RSI.Path.ToString(), layer.State.Name);
                spriteLayerData.Add(layerDatum);
            }
        }

        var evResponse = new PlayerSpriteStateMessage(ev.TargetPlayer, spriteLayerData.ToArray());
        RaiseNetworkEvent(evResponse);
    }

    private void OnPlayerSpriteStateMessage(PlayerSpriteStateMessage ev)
    {
        var hologram = GetEntity(ev.SpriteEntity);

        if (!TryComp<SpriteComponent>(hologram, out var hologramSprite))
            return;

        if (!TryComp<HolopadHologramComponent>(hologram, out var holopadhologram))
            return;

        for (int i = hologramSprite.AllLayers.Count() - 1; i >= 0; i--)
            hologramSprite.RemoveLayer(i);

        var spriteData = ev.SpriteLayerData;

        if (spriteData.Length == 0 &&
            !string.IsNullOrEmpty(holopadhologram.RsiPath) &&
            !string.IsNullOrEmpty(holopadhologram.RsiState))
        {
            spriteData = new SpriteLayerDatum[1];
            spriteData[0] = new SpriteLayerDatum(holopadhologram.RsiPath, holopadhologram.RsiState);
        }

        for (int i = 0; i < spriteData.Length; i++)
        {
            var layer = new PrototypeLayerData();
            layer.RsiPath = spriteData[i].RSIPath;
            layer.State = spriteData[i].RSIState;
            layer.Shader = "unshaded";

            hologramSprite.AddLayer(layer, i);
        }

        UpdateShader(hologram, hologramSprite, holopadhologram);
    }

    private void UpdateShader(EntityUid uid, SpriteComponent sprite, HolopadHologramComponent holopadHologram)
    {
        var instance = _prototypeManager.Index<ShaderPrototype>(holopadHologram.ShaderName).InstanceUnique();
        instance.SetParameter("color1", new Vector3(holopadHologram.Color1.R, holopadHologram.Color1.G, holopadHologram.Color1.B));
        instance.SetParameter("color2", new Vector3(holopadHologram.Color2.R, holopadHologram.Color2.G, holopadHologram.Color2.B));
        instance.SetParameter("alpha", holopadHologram.Alpha);
        instance.SetParameter("intensity", holopadHologram.Intensity);

        sprite.PostShader = instance;
        sprite.RaiseShaderEvent = true;
    }
}
