using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using System.Numerics;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosPipeLayersSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    private Vector2[] _layerOffsets = { new Vector2(0f, 0f), new Vector2(0.21875f, 0f), new Vector2(-0.21875f, 0f) };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosPipeLayersComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<AtmosPipeLayersComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
        SubscribeLocalEvent<AtmosPipeLayersComponent, ActivateInWorldEvent>(OnInteractHandEvent);
    }

    private void OnExamined(Entity<AtmosPipeLayersComponent> ent, ref ExaminedEvent args)
    {
        var layer = Loc.GetString("atmos-pipe-layers-component-layer-" + ent.Comp.CurrentPipeLayer);
        args.PushMarkup(Loc.GetString("atmos-pipe-layers-component-current-layer", ("layer", layer)));
    }

    private void OnGetVerb(Entity<AtmosPipeLayersComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (ent.Comp.PipeLayersLocked)
            return;

        for (var i = 0; i < AtmosPipeLayersComponent.MaxPipeLayer + 1; i++)
        {
            var index = i;
            var layer = Loc.GetString("atmos-pipe-layers-component-layer-" + index);
            var label = Loc.GetString("atmos-pipe-layers-component-select-layer", ("layer", layer));

            var v = new AlternativeVerb
            {
                Priority = 1,
                Category = VerbCategory.ChangePipeLayer,
                Text = label,
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
        if (ent.Comp.PipeLayersLocked)
            return;

        ent.Comp.CurrentPipeLayer = (byte)Math.Clamp(layer, 0, AtmosPipeLayersComponent.MaxPipeLayer);
        Dirty(ent);

        if (TryComp<AppearanceComponent>(ent, out var appearance))
        {
            _appearance.SetData(ent, PipeVisualLayers.Pipe, ent.Comp.LayerVisualStates[ent.Comp.CurrentPipeLayer], appearance);
            _appearance.SetData(ent, PipeVisualLayers.Connector, ent.Comp.ConnectorVisualStates[ent.Comp.CurrentPipeLayer], appearance);
            _appearance.SetData(ent, PipeVisualLayers.Device, _layerOffsets[ent.Comp.CurrentPipeLayer], appearance);
        }
    }
}
