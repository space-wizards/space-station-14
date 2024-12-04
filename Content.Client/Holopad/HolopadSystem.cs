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
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolopadHologramComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HolopadHologramComponent, BeforePostShaderRenderEvent>(OnShaderRender);
        SubscribeAllEvent<TypingChangedEvent>(OnTypingChanged);

        SubscribeNetworkEvent<PlayerSpriteStateRequest>(OnPlayerSpriteStateRequest);
        SubscribeNetworkEvent<PlayerSpriteStateMessage>(OnPlayerSpriteStateMessage);
    }

    private void OnComponentInit(EntityUid uid, HolopadHologramComponent component, ComponentInit ev)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        UpdateHologramSprite(uid);
    }

    private void OnShaderRender(EntityUid uid, HolopadHologramComponent component, BeforePostShaderRenderEvent ev)
    {
        if (ev.Sprite.PostShader == null)
            return;

        ev.Sprite.PostShader.SetParameter("t", (float)_timing.CurTime.TotalSeconds * component.ScrollRate);
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

        var spriteLayerData = new List<PrototypeLayerData>();

        if (playerSprite.Visible)
        {
            for (int i = 0; i < playerSprite.AllLayers.Count(); i++)
            {
                if (!playerSprite.TryGetLayer(i, out var layer))
                    continue;

                if (!layer.Visible ||
                    string.IsNullOrEmpty(layer.ActualRsi?.Path.ToString()) ||
                    string.IsNullOrEmpty(layer.State.Name))
                    continue;

                var layerDatum = new PrototypeLayerData();
                layerDatum.RsiPath = layer.ActualRsi.Path.ToString();
                layerDatum.State = layer.State.Name;

                if (layer.CopyToShaderParameters != null)
                {
                    var key = (string)layer.CopyToShaderParameters.LayerKey;

                    if (playerSprite.LayerMapTryGet(key, out var otherLayerIdx) &&
                        playerSprite.TryGetLayer(otherLayerIdx, out var otherLayer) &&
                        otherLayer.Visible)
                    {
                        layerDatum.MapKeys = new() { key };

                        layerDatum.CopyToShaderParameters = new PrototypeCopyToShaderParameters()
                        {
                            LayerKey = key,
                            ParameterTexture = layer.CopyToShaderParameters.ParameterTexture,
                            ParameterUV = layer.CopyToShaderParameters.ParameterUV
                        };
                    }
                }

                spriteLayerData.Add(layerDatum);
            }
        }

        var evResponse = new PlayerSpriteStateMessage(ev.TargetPlayer, spriteLayerData.ToArray());
        RaiseNetworkEvent(evResponse);
    }

    private void OnPlayerSpriteStateMessage(PlayerSpriteStateMessage ev)
    {
        UpdateHologramSprite(GetEntity(ev.SpriteEntity), ev.SpriteLayerData);
    }

    private void UpdateHologramSprite(EntityUid uid, PrototypeLayerData[]? layerData = null)
    {
        if (!TryComp<SpriteComponent>(uid, out var hologramSprite))
            return;

        if (!TryComp<HolopadHologramComponent>(uid, out var holopadhologram))
            return;

        for (int i = hologramSprite.AllLayers.Count() - 1; i >= 0; i--)
            hologramSprite.RemoveLayer(i);

        if (layerData == null || layerData.Length == 0)
        {
            layerData = new PrototypeLayerData[1];
            layerData[0] = new PrototypeLayerData()
            {
                RsiPath = holopadhologram.RsiPath,
                State = holopadhologram.RsiState
            };
        }

        for (int i = 0; i < layerData.Length; i++)
        {
            var layer = layerData[i];
            layer.Shader = "unshaded";

            hologramSprite.AddLayer(layerData[i], i);
        }

        UpdateHologramShader(uid, hologramSprite, holopadhologram);
    }

    private void UpdateHologramShader(EntityUid uid, SpriteComponent sprite, HolopadHologramComponent holopadHologram)
    {
        // Find the texture height of the largest layer
        var texHeight = sprite.AllLayers.Max(x => x.PixelSize.Y);

        var instance = _prototypeManager.Index<ShaderPrototype>(holopadHologram.ShaderName).InstanceUnique();
        instance.SetParameter("color1", new Vector3(holopadHologram.Color1.R, holopadHologram.Color1.G, holopadHologram.Color1.B));
        instance.SetParameter("color2", new Vector3(holopadHologram.Color2.R, holopadHologram.Color2.G, holopadHologram.Color2.B));
        instance.SetParameter("alpha", holopadHologram.Alpha);
        instance.SetParameter("intensity", holopadHologram.Intensity);
        instance.SetParameter("texHeight", texHeight);
        instance.SetParameter("t", (float)_timing.CurTime.TotalSeconds * holopadHologram.ScrollRate);

        sprite.PostShader = instance;
        sprite.RaiseShaderEvent = true;
    }
}
