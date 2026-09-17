using System.Globalization;
using System.Numerics;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Content.Shared.Decals;

public sealed partial class DecalChunkDecalsSerializer : ITypeSerializer<Dictionary<ushort, Decal>, MappingDataNode>
{
    private const int VersionGroupedByData = 1;

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        if (!TryGetVersion(node, out var version))
        {
            return new DictionarySerializer<ushort, Decal>()
                .Validate(serializationManager, node, dependencies, context);
        }

        return version switch
        {
            VersionGroupedByData => new InconclusiveNode(node),
            _ => new ErrorNode(node, $"Unsupported decal chunk decals version {version}."),
        };
    }

    public Dictionary<ushort, Decal> Read(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<Dictionary<ushort, Decal>>? instanceProvider = default)
    {
        if (!TryGetVersion(node, out var version))
        {
            return new DictionarySerializer<ushort, Decal>()
                .Read(serializationManager, node, dependencies, hookCtx, context, instanceProvider);
        }

        return version switch
        {
            VersionGroupedByData => ReadGroupedByData(serializationManager, node, hookCtx, context, instanceProvider),
            _ => throw new InvalidOperationException($"Unsupported decal chunk decals version {version}."),
        };
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        Dictionary<ushort, Decal> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var groups = new SortedDictionary<DecalData, List<(ushort Id, Vector2 Coordinates)>>();

        foreach (var (id, decal) in value)
        {
            var data = new DecalData(decal);
            groups.GetOrNew(data).Add((id, decal.Coordinates));
        }

        var nodes = new SequenceDataNode();

        foreach (var (data, decals) in groups)
        {
            decals.Sort((x, y) => x.Id.CompareTo(y.Id));

            var decalNodes = new MappingDataNode();
            foreach (var (id, coordinates) in decals)
            {
                decalNodes.Add(
                    id.ToString(CultureInfo.InvariantCulture),
                    serializationManager.WriteValue(coordinates, alwaysWrite, context));
            }

            nodes.Add(new MappingDataNode
            {
                { "node", serializationManager.WriteValue(data, alwaysWrite, context) },
                { "decals", decalNodes },
            });
        }

        return new MappingDataNode
        {
            { "version", VersionGroupedByData.ToString(CultureInfo.InvariantCulture) },
            { "nodes", nodes },
        };
    }

    private static Dictionary<ushort, Decal> ReadGroupedByData(
        ISerializationManager serializationManager,
        MappingDataNode node,
        SerializationHookContext hookCtx,
        ISerializationContext? context,
        ISerializationManager.InstantiationDelegate<Dictionary<ushort, Decal>>? instanceProvider)
    {
        var dictionary = instanceProvider != null ? instanceProvider() : new Dictionary<ushort, Decal>();
        var nodes = (SequenceDataNode) node["nodes"];

        foreach (var dNode in nodes)
        {
            var aNode = (MappingDataNode) dNode;
            var data = serializationManager.Read<DecalData>(aNode["node"], hookCtx, context);
            var decalNodes = (MappingDataNode) aNode["decals"];

            foreach (var (idNode, coordinatesNode) in decalNodes)
            {
                var id = ushort.Parse(idNode, CultureInfo.InvariantCulture);
                var coordinates = serializationManager.Read<Vector2>(coordinatesNode, hookCtx, context);
                dictionary[id] = new Decal(coordinates, data.Id, data.Color, data.Angle, data.ZIndex, data.Cleanable);
            }
        }

        return dictionary;
    }

    private static bool TryGetVersion(MappingDataNode node, out int version)
    {
        if (node.TryGetValue("version", out var versionNode) &&
            versionNode is ValueDataNode value)
        {
            version = value.AsInt();
            return true;
        }

        version = default;
        return false;
    }

    [DataDefinition]
    private readonly partial struct DecalData : IEquatable<DecalData>, IComparable<DecalData>
    {
        [DataField("id")]
        public string Id { get; init; } = string.Empty;

        [DataField("color")]
        public Color? Color { get; init; }

        [DataField("angle")]
        public Angle Angle { get; init; } = Angle.Zero;

        [DataField("zIndex")]
        public int ZIndex { get; init; }

        [DataField("cleanable")]
        public bool Cleanable { get; init; }

        public DecalData(string id, Color? color, Angle angle, int zIndex, bool cleanable)
        {
            Id = id;
            Color = color;
            Angle = angle;
            ZIndex = zIndex;
            Cleanable = cleanable;
        }

        public DecalData(Decal decal)
        {
            Id = decal.Id;
            Color = decal.Color;
            Angle = decal.Angle;
            ZIndex = decal.ZIndex;
            Cleanable = decal.Cleanable;
        }

        public bool Equals(DecalData other)
        {
            return Id == other.Id &&
                   Nullable.Equals(Color, other.Color) &&
                   Angle.Equals(other.Angle) &&
                   ZIndex == other.ZIndex &&
                   Cleanable == other.Cleanable;
        }

        public override bool Equals(object? obj)
        {
            return obj is DecalData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Color, Angle, ZIndex, Cleanable);
        }

        public int CompareTo(DecalData other)
        {
            var idComparison = string.Compare(Id, other.Id, StringComparison.Ordinal);
            if (idComparison != 0)
                return idComparison;

            var colorComparison = string.Compare(Color?.ToHex(), other.Color?.ToHex(), StringComparison.Ordinal);
            if (colorComparison != 0)
                return colorComparison;

            var angleComparison = Angle.Theta.CompareTo(other.Angle.Theta);
            if (angleComparison != 0)
                return angleComparison;

            var zIndexComparison = ZIndex.CompareTo(other.ZIndex);
            if (zIndexComparison != 0)
                return zIndexComparison;

            return Cleanable.CompareTo(other.Cleanable);
        }
    }
}
