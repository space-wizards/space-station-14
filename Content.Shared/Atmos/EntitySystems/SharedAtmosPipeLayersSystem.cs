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
        SubscribeLocalEvent<AtmosPipeLayersComponent, TrySetNextPipeLayerCompletedEvent>(OnSetNextPipeLayerCompleted);
        SubscribeLocalEvent<AtmosPipeLayersComponent, TrySettingPipeLayerCompletedEvent>(OnSettingPipeLayerCompleted);
    }

    private void OnExamined(Entity<AtmosPipeLayersComponent> ent, ref ExaminedEvent args)
    {
        var layerName = GetPipeLayerName(ent.Comp.CurrentPipeLayer);
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

        if (TryComp<SubFloorHideComponent>(ent, out var subFloorHide) && subFloorHide.IsUnderCover)
        {
            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.Adjust,
                Text = Loc.GetString("atmos-pipe-layers-component-pipes-are-covered"),
                Disabled = true,
                Impact = LogImpact.Low,
                DoContactInteraction = true,
            };

            args.Verbs.Add(v);
        }

        else if (!TryGetHeldTool(user, ent.Comp.Tool, out var tool))
        {
            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.Adjust,
                Text = Loc.GetString("atmos-pipe-layers-component-tool-missing", ("toolName", Loc.GetString(toolProto.ToolName).ToLower())),
                Disabled = true,
                Impact = LogImpact.Low,
                DoContactInteraction = true,
            };

            args.Verbs.Add(v);
        }

        else
        {
            for (var i = 0; i < ent.Comp.NumberOfPipeLayers; i++)
            {
                var index = i;
                var layerName = GetPipeLayerName((AtmosPipeLayer)index);
                var label = Loc.GetString("atmos-pipe-layers-component-select-layer", ("layerName", layerName));

                var v = new Verb
                {
                    Priority = 1,
                    Category = VerbCategory.Adjust,
                    Text = label,
                    Disabled = index == (int)ent.Comp.CurrentPipeLayer,
                    Impact = LogImpact.Low,
                    DoContactInteraction = true,
                    Act = () =>
                    {
                        _tool.UseTool(tool.Value, user, ent, ent.Comp.Delay, tool.Value.Comp.Qualities, new TrySettingPipeLayerCompletedEvent((AtmosPipeLayer)index));
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

        if (!TryComp<ToolComponent>(args.Used, out var tool) || !_tool.HasQuality(args.Used, ent.Comp.Tool, tool))
            return;

        if (TryComp<SubFloorHideComponent>(ent, out var subFloorHide) && subFloorHide.IsUnderCover)
        {
            _popup.PopupClient(Loc.GetString("atmos-pipe-layers-component-cannot-adjust-pipes"), ent, args.User);
            return;
        }

        _tool.UseTool(args.Used, args.User, ent, ent.Comp.Delay, tool.Qualities, new TrySetNextPipeLayerCompletedEvent());
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

                _popup.PopupClient(message, ent, args.User);
            }

            return;
        }

        _tool.UseTool(tool.Value, args.User, ent, ent.Comp.Delay, tool.Value.Comp.Qualities, new TrySetNextPipeLayerCompletedEvent());
    }

    private void OnSetNextPipeLayerCompleted(Entity<AtmosPipeLayersComponent> ent, ref TrySetNextPipeLayerCompletedEvent args)
    {
        if (args.Cancelled)
            return;

        SetNextPipeLayer(ent, args.User, args.Used);
    }

    private void OnSettingPipeLayerCompleted(Entity<AtmosPipeLayersComponent> ent, ref TrySettingPipeLayerCompletedEvent args)
    {
        if (args.Cancelled)
            return;

        SetPipeLayer(ent, args.PipeLayer, args.User, args.Used);
    }

    /// <summary>
    /// Increments an entity's pipe layer by 1, wrapping around to 0 if the max pipe layer is reached
    /// </summary>
    /// <param name="ent">The pipe entity</param>
    /// <param name="user">The player entity who adjusting the pipe layer</param>
    /// <param name="used">The tool used to adjust the pipe layer</param>
    public void SetNextPipeLayer(Entity<AtmosPipeLayersComponent> ent, EntityUid? user = null, EntityUid? used = null)
    {
        var newLayer = ((int)ent.Comp.CurrentPipeLayer + 1) % ent.Comp.NumberOfPipeLayers;
        SetPipeLayer(ent, (AtmosPipeLayer)newLayer, user, used);
    }

    /// <summary>
    /// Sets an entity's pipe layer to a specified value
    /// </summary>
    /// <param name="ent">The pipe entity</param>
    /// <param name="layer">The new layer value</param>
    /// <param name="user">The player entity who adjusting the pipe layer</param>
    /// <param name="used">The tool used to adjust the pipe layer</param>
    public virtual void SetPipeLayer(Entity<AtmosPipeLayersComponent> ent, AtmosPipeLayer layer, EntityUid? user = null, EntityUid? used = null)
    {
        if (ent.Comp.PipeLayersLocked)
            return;

        ent.Comp.CurrentPipeLayer = (AtmosPipeLayer)Math.Clamp((int)layer, 0, ent.Comp.NumberOfPipeLayers - 1);
        Dirty(ent);

        if (TryComp<AppearanceComponent>(ent, out var appearance))
        {
            if (ent.Comp.SpriteRsiPaths.TryGetValue(ent.Comp.CurrentPipeLayer, out var path))
                _appearance.SetData(ent, AtmosPipeLayerVisuals.Sprite, path, appearance);

            if (ent.Comp.SpriteLayersRsiPaths.Count > 0)
            {
                var data = new Dictionary<string, string>();

                foreach (var (layerKey, rsiPaths) in ent.Comp.SpriteLayersRsiPaths)
                {
                    if (rsiPaths.TryGetValue(ent.Comp.CurrentPipeLayer, out path))
                        data.TryAdd(layerKey, path);
                }

                _appearance.SetData(ent, AtmosPipeLayerVisuals.SpriteLayers, data, appearance);
            }
        }

        if (user != null)
        {
            var layerName = GetPipeLayerName(ent.Comp.CurrentPipeLayer);
            var message = Loc.GetString("atmos-pipe-layers-component-change-layer", ("layerName", layerName));

            _popup.PopupClient(message, ent, user);
        }
    }

    /// <summary>
    /// Try to find an entity prototype associated with a specified <see cref="AtmosPipeLayer"/>.
    /// </summary>
    /// <param name="component">The <see cref="AtmosPipeLayersComponent"/> with the alternative prototypes data.</param>
    /// <param name="layer">The atmos pipe layer associated with the entity prototype.</param>
    /// <param name="proto">The returned entity prototype.</param>
    /// <returns>True if there was an entity prototype associated with the layer.</returns>
    public bool TryGetAlternativePrototype(AtmosPipeLayersComponent component, AtmosPipeLayer layer, out EntProtoId proto)
    {
        return component.AlternativePrototypes.TryGetValue(layer, out proto);
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

    private string GetPipeLayerName(AtmosPipeLayer layer)
    {
        return Loc.GetString("atmos-pipe-layers-component-layer-" + layer.ToString().ToLower());
    }
}
