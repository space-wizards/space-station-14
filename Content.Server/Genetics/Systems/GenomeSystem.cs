using Content.Server.Genetics.Components;
using Content.Shared.GameTicking;
using Content.Shared.Genetics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Genetics.Systems;

/// <summary>
/// Assigns each <see cref="GenomePrototype"/> a random <see cref="GenomeLayout"/> roundstart.
/// </summary>
public sealed class GenomeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    // This is where all the genome layouts are stored.
    // TODO: store on round entity when thats done, so persistence reloading doesnt scramble genes
    [ViewVariables]
    private readonly Dictionary<string, GenomeLayout> _layouts = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GenomeComponent, MapInitEvent>(OnInit);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
    }

    private void OnInit(EntityUid uid, GenomeComponent comp, MapInitEvent args)
    {
        // only empty in test and when vving
        if (comp.GenomeId != string.Empty)
            comp.Layout = GetOrCreateLayout(comp.GenomeId);
    }

    private void Reset(RoundRestartCleanupEvent args)
    {
        _layouts.Clear();
    }

    /// <summary>
    /// Either gets an existing genome layout or creates a new random one.
    /// Genome layouts are reset between rounds.
    /// Anything with <see cref="GenomeComponent"/> calls this on mapinit to ensure it uses the correct layout.
    /// </summary>
    /// <param name="id">Genome prototype id to create the layout from</param>
    public GenomeLayout GetOrCreateLayout(string id)
    {
        // already created for this round so just use it
        if (TryGetLayout(id, out var layout))
            return layout;

        // prototype must exist
        var proto = _proto.Index<GenomePrototype>(id);

        // create the new random genome layout!
        layout = new GenomeLayout();
        var names = new List<string>(proto.ValueBits.Keys);
        _random.Shuffle(names);
        foreach (var name in names)
        {
            var length = proto.ValueBits[name];
            layout.Add(name, length);
        }

        // save it for the rest of the round
        AddLayout(id, layout);
        return layout;
    }

    /// <summary>
    /// Sets the <c>Genome</c> bits from a <see cref="GenesPrototype"/>'s values.
    /// </summary>
    public void LoadGenes(EntityUid uid, ProtoId<GenesPrototype> id, GenomeComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        var genes = _proto.Index(id);
        foreach (var name in genes.Bools)
        {
            comp.Layout.SetBool(comp.Genome, name, true);
        }

        foreach (var (name, value) in genes.Ints)
        {
            comp.Layout.SetInt(comp.Genome, name, value);
        }
    }

    /// <summary>
    /// Copies the <c>Genome</c> bits from a parent to a child.
    /// They must use the same genome layout or it will be logged and copy nothing.
    /// </summary>
    public void CopyParentGenes(EntityUid uid, EntityUid parent, GenomeComponent? comp = null, GenomeComponent? parentComp = null)
    {
        if (!Resolve(uid, ref comp) || !Resolve(parent, ref parentComp))
            return;

        if (parentComp.GenomeId != comp.GenomeId)
        {
            Log.Error($"Tried to copy incompatible genome from {ToPrettyString(parent):parent)} ({parentComp.GenomeId}) to {ToPrettyString(uid):child)} ({comp.GenomeId})");
            return;
        }

        parentComp.Genome.CopyTo(comp.Genome);
    }

    private bool TryGetLayout(string id, [NotNullWhen(true)] out GenomeLayout? layout)
    {
        return _layouts.TryGetValue(id, out layout);
    }

    private void AddLayout(string id, GenomeLayout layout)
    {
        _layouts.Add(id, layout);
    }
}
