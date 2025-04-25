using Content.Shared.Atmos.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.SubFloor;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Atmos.EntitySystems;

/// <summary>
/// The system responsible for checking and adjusting the connection layering of gas pipes
/// </summary>
public abstract partial class SharedAtmosPipeLayersSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosPipeLayersComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<AtmosPipeLayersComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<AtmosPipeLayersComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AtmosPipeLayersComponent, UseInHandEvent>(OnUseInHandEvent);
        SubscribeLocalEvent<AtmosPipeLayersComponent, TryCyclingPipeLayerCompletedEvent>(OnCyclingPipeLayerCompleted);
        SubscribeLocalEvent<AtmosPipeLayersComponent, TrySettingPipeLayerCompletedEvent>(OnSettingPipeLayerCompleted);
    }

    private void OnExamined(Entity<AtmosPipeLayersComponent> ent, ref ExaminedEvent args)
    {
        var layerName = Loc.GetString("atmos-pipe-layers-component-layer-" + ent.Comp.CurrentPipeLayer);
        args.PushMarkup(Loc.GetString("atmos-pipe-layers-component-current-layer", ("layerName", layerName)));
    }

    private void OnGetVerb(Entity<AtmosPipeLayersComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (ent.Comp.NumberOfPipeLayers <= 1 || ent.Comp.PipeLayersLocked)
            return;

        if (!_protoManager.TryIndex(ent.Comp.Tool, out var toolProto))
            return;

        var user = args.User;
        var isCovered = TryComp<SubFloorHideComponent>(ent, out var subFloorHide) && subFloorHide.IsUnderCover;
        var isMissingTool = !TryGetHeldTool(user, ent.Comp.Tool, out var tool);

        if (isCovered || isMissingTool)
        {
            var label = "???";

            if (isCovered)
            {
                label = Loc.GetString("atmos-pipe-layers-component-pipes-are-covered");
            }

            else if (isMissingTool)
            {
                var toolName = Loc.GetString(toolProto.ToolName).ToLower();
                label = Loc.GetString("atmos-pipe-layers-component-tool-missing", ("toolName", toolName));
            }

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.Adjust,
                Text = label,
                Disabled = true,
                Impact = LogImpact.Low,
            };

            args.Verbs.Add(v);
        }

        else if (tool != null)
        {
            for (var i = 0; i < ent.Comp.NumberOfPipeLayers; i++)
            {
                var index = i;
                var layerName = Loc.GetString("atmos-pipe-layers-component-layer-" + index);
                var label = Loc.GetString("atmos-pipe-layers-component-select-layer", ("layerName", layerName));

                var v = new Verb
                {
                    Priority = 1,
                    Category = VerbCategory.Adjust,
                    Text = label,
                    Disabled = index == ent.Comp.CurrentPipeLayer,
                    Impact = LogImpact.Low,
                    DoContactInteraction = true,
                    Act = () =>
                    {
                        _tool.UseTool(tool.Value, user, ent, ent.Comp.Delay, tool.Value.Comp.Qualities, new TrySettingPipeLayerCompletedEvent(index));
                    }
                };

                args.Verbs.Add(v);
            }
        }
    }

    private void OnInteractUsing(Entity<AtmosPipeLayersComponent> ent, ref InteractUsingEvent args)
    {
        if (ent.Comp.NumberOfPipeLayers <= 1 || ent.Comp.PipeLayersLocked)
            return;

        if (TryComp<SubFloorHideComponent>(ent, out var subFloorHide) && subFloorHide.IsUnderCover)
        {
            _popup.PopupPredicted(Loc.GetString("atmos-pipe-layers-component-cannot-adjust-pipes"), ent, args.User);
            return;
        }

        if (TryComp<ToolComponent>(args.Used, out var tool) && _tool.HasQuality(args.Used, ent.Comp.Tool, tool))
            _tool.UseTool(args.Used, args.User, ent, ent.Comp.Delay, tool.Qualities, new TryCyclingPipeLayerCompletedEvent());
    }

    private void OnUseInHandEvent(Entity<AtmosPipeLayersComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.NumberOfPipeLayers <= 1 || ent.Comp.PipeLayersLocked)
            return;

        if (!TryGetHeldTool(args.User, ent.Comp.Tool, out var tool))
        {
            if (_protoManager.TryIndex(ent.Comp.Tool, out var toolProto))
            {
                var toolName = Loc.GetString(toolProto.ToolName).ToLower();
                var message = Loc.GetString("atmos-pipe-layers-component-tool-missing", ("toolName", toolName));

                _popup.PopupPredicted(message, ent, args.User);
            }

            return;
        }

        _tool.UseTool(tool.Value, args.User, ent, ent.Comp.Delay, tool.Value.Comp.Qualities, new TryCyclingPipeLayerCompletedEvent());
    }

    private void OnCyclingPipeLayerCompleted(Entity<AtmosPipeLayersComponent> ent, ref TryCyclingPipeLayerCompletedEvent args)
    {
        if (args.Cancelled)
            return;

        CyclePipeLayer(ent, args.User);
    }

    private void OnSettingPipeLayerCompleted(Entity<AtmosPipeLayersComponent> ent, ref TrySettingPipeLayerCompletedEvent args)
    {
        if (args.Cancelled)
            return;

        SetPipeLayer(ent, args.PipeLayer, args.User);
    }

    /// <summary>
    /// Increments an entity's pipe layer by 1, wrapping around to 0 if the max pipe layer is reached
    /// </summary>
    /// <param name="ent">The pipe entity</param>
    /// <param name="user">The player entity who adjusting the pipe layer</param>
    public void CyclePipeLayer(Entity<AtmosPipeLayersComponent> ent, EntityUid? user = null)
    {
        var newLayer = (ent.Comp.CurrentPipeLayer + 1) % ent.Comp.NumberOfPipeLayers;
        SetPipeLayer(ent, (byte)newLayer, user);
    }

    /// <summary>
    /// Sets an entity's pipe layer to a specified value
    /// </summary>
    /// <param name="ent">The pipe entity</param>
    /// <param name="layer">The new layer value</param>
    /// <param name="user">The player entity who adjusting the pipe layer</param>
    public virtual void SetPipeLayer(Entity<AtmosPipeLayersComponent> ent, int layer, EntityUid? user = null)
    {
        if (ent.Comp.PipeLayersLocked)
            return;

        ent.Comp.CurrentPipeLayer = (byte)Math.Clamp(layer, 0, ent.Comp.NumberOfPipeLayers - 1);
        Dirty(ent);

        if (TryComp<AppearanceComponent>(ent, out var appearance))
        {
            if (ent.Comp.CurrentPipeLayer < ent.Comp.SpriteRsiPaths.Length)
                _appearance.SetData(ent, AtmosPipeLayerVisuals.Sprite, ent.Comp.SpriteRsiPaths[ent.Comp.CurrentPipeLayer], appearance);

            if (ent.Comp.SpriteLayersRsiPaths.Count > 0)
            {
                var data = new Dictionary<string, string>();

                foreach (var (layerKey, rsiPaths) in ent.Comp.SpriteLayersRsiPaths)
                {
                    if (ent.Comp.CurrentPipeLayer < rsiPaths.Length)
                        data.TryAdd(layerKey, rsiPaths[ent.Comp.CurrentPipeLayer]);
                }

                _appearance.SetData(ent, AtmosPipeLayerVisuals.SpriteLayers, data, appearance);
            }
        }

        if (user != null)
        {
            var layerName = Loc.GetString("atmos-pipe-layers-component-layer-" + ent.Comp.CurrentPipeLayer);
            var message = Loc.GetString("atmos-pipe-layers-component-change-layer", ("layerName", layerName));

            _popup.PopupPredicted(message, ent, user);
        }
    }

    /// <summary>
    /// Checks a player entity's hands to see if they are holding a tool with a specified quality
    /// </summary>
    /// <param name="user">The player entity</param>
    /// <param name="toolQuality">The tool quality being checked for</param>
    /// <param name="heldTool">A tool with the specified tool quality</param>
    /// <returns>True if an appropriate tool was found</returns>
    private bool TryGetHeldTool(EntityUid user, ProtoId<ToolQualityPrototype> toolQuality, [NotNullWhen(true)] out Entity<ToolComponent>? heldTool)
    {
        heldTool = null;

        foreach (var heldItem in _hands.EnumerateHeld(user))
        {
            if (TryComp<ToolComponent>(heldItem, out var tool) &&
                _tool.HasQuality(heldItem, toolQuality, tool))
            {
                heldTool = new Entity<ToolComponent>(heldItem, tool);
                return true;
            }
        }

        return false;
    }
}
