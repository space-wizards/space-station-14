using Content.Server.Genetics.Components;
using Content.Shared.GameTicking;
using Content.Shared.Genetics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Genetics.Systems;

/// <summary>
/// Assigns each <see cref="GenomePrototype"/> a random <see cref="GenomeLayout"/> roundstart.
/// </summary>
public sealed class GenomeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;
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

    private void OnInit(Entity<GenomeComponent> ent, ref MapInitEvent args)
    {
        // only empty in test and when vving
        if (ent.Comp.GenomeId == string.Empty)
            return;

        ent.Comp.Layout = GetOrCreateLayout(ent.Comp.GenomeId);
        ent.Comp.Genome = new Genome(ent.Comp.Layout.TotalBits);
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

        foreach (var (name, typeName) in proto.Prototypes)
        {
            if (!proto.PrototypeIds.TryGetValue(typeName, out var ids))
            {
                Log.Error($"Type {typeName} was listed in prototypes of {id} but not prototypeIds!");
                continue;
            }

            var bits = (ushort) Math.Ceiling(Math.Log2(ids.Count));
            layout.Add(name, bits);
        }

        foreach (var (typeName, ids) in proto.PrototypeIds)
        {
            if (!_reflection.TryLooseGetType(typeName, out var type))
            {
                Log.Error($"Found invalid type {typeName} in prototypeIds of {id}!");
                continue;
            }

            // randomize prototype genome value indices
            names.Clear();
            names.AddRange(ids);
            _random.Shuffle(names);
            var protoLayout = new GenomePrototypesLayout();
            foreach (var protoId in names)
            {
                if (!_proto.TryIndex(type, protoId, out _))
                {
                    Log.Error($"Unknown prototype {protoId} for {typeName} found in prototypeIds of {id}!");
                    continue;
                }

                protoLayout.Add(protoId);
            }
            layout.SetPrototypesLayout(typeName, protoLayout);
        }

        // save it for the rest of the round
        AddLayout(id, layout);
        return layout;
    }

    /// <summary>
    /// Sets the <c>Genome</c> bits from a <see cref="GenesPrototype"/>'s values.
    /// </summary>
    public void LoadGenes(Entity<GenomeComponent?> ent, ProtoId<GenesPrototype> id)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!_proto.TryIndex<GenomePrototype>(ent.Comp.GenomeId, out var proto))
            return;

        var layout = ent.Comp.Layout;
        var genome = ent.Comp.Genome;

        var genes = _proto.Index(id);
        foreach (var name in genes.Bools)
        {
            layout.SetBool(genome, name, true);
        }

        foreach (var (name, value) in genes.Ints)
        {
            layout.SetInt(genome, name, value);
        }

        foreach (var (name, protoId) in genes.Prototypes)
        {
            if (!proto.Prototypes.TryGetValue(name, out var typeName))
            {
                Log.Error($"GenesPrototype {id} had undeclared prototype value {name} listed!");
                continue;
            }

            // this is logged when loading genome prototype, no need to duplicate it
            if (!_reflection.TryLooseGetType(typeName, out var type))
                continue;

            if (!_proto.TryIndex(type, id, out _))
            {
                Log.Error($"GenesPrototype {id} had unknown prototype id {protoId} for {name} listed!");
                continue;
            }

            layout.SetPrototype(genome, name, typeName, protoId);
        }
    }

    /// <summary>
    /// Copies the <c>Genome</c> bits from a parent to a child, asexually.
    /// They must use the same genome layout or it will be logged and copy nothing.
    /// </summary>
    public void CopyParentGenes(Entity<GenomeComponent?> child, Entity<GenomeComponent?> parent)
    {
        if (!Resolve(child, ref child.Comp) || !Resolve(parent, ref parent.Comp))
            return;

        if (parent.Comp.GenomeId != child.Comp.GenomeId)
        {
            Log.Error($"Tried to copy incompatible genome from {ToPrettyString(parent):parent)} ({parent.Comp.GenomeId}) to {ToPrettyString(child):child)} ({child.Comp.GenomeId})");
            return;
        }

        parent.Comp.Genome.CopyTo(child.Comp.Genome);
    }

    /// <summary>
    /// Mixes parent genes together randomly to a child.
    /// They must all use the same genome layout or it will be logged and copy nothing.
    /// </summary>
    public void MixParentGenes(Entity<GenomeComponent?> child, Entity<GenomeComponent?> mother, Entity<GenomeComponent?> father)
    {
        if (!Resolve(child, ref child.Comp) || !Resolve(mother, ref mother.Comp) || !Resolve(father, ref father.Comp))
            return;

        if (mother.Comp.GenomeId != father.Comp.GenomeId)
        {
            Log.Error($"Tried to mix incompatible genome from {ToPrettyString(mother):mother)} ({mother.Comp.GenomeId}) and {ToPrettyString(father):father)} ({father.Comp.GenomeId})");
            return;
        }

        if (mother.Comp.GenomeId != child.Comp.GenomeId)
        {
            Log.Error($"Tried to mix incompatible genome from {ToPrettyString(mother):mother)} and {ToPrettyString(father):father)} ({father.Comp.GenomeId}) onto {ToPrettyString(child):child)} ({child.Comp.GenomeId})");
            return;
        }

        child.Comp.Layout.MixGenes(child.Comp.Genome, mother.Comp.Genome, father.Comp.Genome, _random);
    }

    private bool TryGetLayout(string id, [NotNullWhen(true)] out GenomeLayout? layout)
    {
        return _layouts.TryGetValue(id, out layout);
    }

    private void AddLayout(string id, GenomeLayout layout)
    {
        _layouts.Add(id, layout);
    }

    #region "GenomeLayout wrappers"

    public int GetInt(GenomeComponent comp, string name)
    {
        return comp.Layout.GetInt(comp.Genome, name);
    }

    public bool GetBool(GenomeComponent comp, string name)
    {
        return comp.Layout.GetBool(comp.Genome, name);
    }

    public IPrototype? GetPrototype(GenomeComponent comp, string typeName, string name)
    {
        return comp.Layout.GetPrototype(comp.Genome, typeName, name, _proto, _reflection);
    }

    #endregion
}
