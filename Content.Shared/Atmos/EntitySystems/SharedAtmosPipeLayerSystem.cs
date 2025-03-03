using Content.Shared.Atmos.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Verbs;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosPipeLayerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosPipeLayerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<AtmosPipeLayerComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<AtmosPipeLayerComponent, ActivateInWorldEvent>(OnInteractHandEvent);
    }

    private void OnExamined(Entity<AtmosPipeLayerComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup("Current layer: " + ent.Comp.CurrentPipeLayer);
    }

    private void OnGetVerb(Entity<AtmosPipeLayerComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        for (var i = AtmosPipeLayerComponent.MinPipeLayer; i < AtmosPipeLayerComponent.MaxPipeLayer; i++)
        {
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
                    SetPipeLayer(ent, i);
                }
            };

            args.Verbs.Add(v);
        }
    }

    private void OnInteractHandEvent(Entity<AtmosPipeLayerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!args.Complex)
            return;

        CyclePipeLayer(ent);
    }

    public void CyclePipeLayer(Entity<AtmosPipeLayerComponent> ent)
    {
        var newLayer = ent.Comp.CurrentPipeLayer + 1;

        if (newLayer > AtmosPipeLayerComponent.MaxPipeLayer)
            newLayer = AtmosPipeLayerComponent.MinPipeLayer;

        SetPipeLayer(ent, newLayer);
    }

    public virtual void SetPipeLayer(Entity<AtmosPipeLayerComponent> ent, int layer)
    {
        ent.Comp.CurrentPipeLayer = Math.Clamp(layer, AtmosPipeLayerComponent.MinPipeLayer, AtmosPipeLayerComponent.MaxPipeLayer);

        Dirty(ent);
    }
}
