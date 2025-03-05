using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.SubFloor;
using Content.Shared.Verbs;
using System.Numerics;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosPipeLayersSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    private Vector2[] _layerOffsets = { new Vector2(-0.21875f, 0f), new Vector2(0f, 0f), new Vector2(0.21875f, 0f) };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosPipeLayersComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<AtmosPipeLayersComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<AtmosPipeLayersComponent, ActivateInWorldEvent>(OnInteractHandEvent);
    }

    private void OnExamined(Entity<AtmosPipeLayersComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup("Current layer: " + ent.Comp.CurrentPipeLayer);
    }

    private void OnGetVerb(Entity<AtmosPipeLayersComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (ent.Comp.PipeLayersLocked)
            return;

        for (var i = 0; i < AtmosPipeLayersComponent.MaxPipeLayer + 1; i++)
        {
            var index = i;

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = "Layer " + i,
                Disabled = i == ent.Comp.CurrentPipeLayer,
                Impact = LogImpact.Low,
                DoContactInteraction = true,
                Act = () =>
                {
                    SetPipeLayer(ent, index);
                }
            };

            args.Verbs.Add(v);
        }
    }

    private void OnInteractHandEvent(Entity<AtmosPipeLayersComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!args.Complex)
            return;

        if (!_handsSystem.IsHolding(args.User, ent))
            return;

        CyclePipeLayer(ent);
    }

    public void CyclePipeLayer(Entity<AtmosPipeLayersComponent> ent)
    {
        var newLayer = ent.Comp.CurrentPipeLayer + 1;

        if (newLayer > AtmosPipeLayersComponent.MaxPipeLayer)
            newLayer = 0;

        SetPipeLayer(ent, newLayer);
    }

    public virtual void SetPipeLayer(Entity<AtmosPipeLayersComponent> ent, int layer)
    {
        if (layer == ent.Comp.CurrentPipeLayer)
            return;

        if (ent.Comp.PipeLayersLocked)
            return;

        ent.Comp.CurrentPipeLayer = Math.Clamp(layer, 0, AtmosPipeLayersComponent.MaxPipeLayer);
        Dirty(ent);

        if (TryComp<AppearanceComponent>(ent, out var appearance))
        {
            _appearance.SetData(ent, PipeVisualLayers.Pipe, ent.Comp.LayerVisualStates[ent.Comp.CurrentPipeLayer], appearance);
            _appearance.SetData(ent, PipeVisualLayers.Connector, ent.Comp.ConnectorVisualStates[ent.Comp.CurrentPipeLayer], appearance);

            if (ent.Comp.OffsetAboveFloorLayers)
                _appearance.SetData(ent, SubfloorLayers.FirstLayer, _layerOffsets[ent.Comp.CurrentPipeLayer], appearance);
        }
    }
}
