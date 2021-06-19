using System.Collections.Generic;
using System.Collections.Immutable;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Body.Surgery.Operation.Step.Serializers
{
    public class OperationStepImmutableListSerializer : ITypeSerializer<ImmutableList<OperationStep>, SequenceDataNode>
    {
        private readonly OperationStepSerializer _operationStepSerializer = new();

        public ValidationNode Validate(
            ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            var list = new List<ValidationNode>();

            foreach (var dataNode in node.Sequence)
            {
                if (dataNode is not ValueDataNode value)
                {
                    list.Add(new ErrorNode(dataNode, $"Cannot cast node {dataNode} to ValueDataNode."));
                    continue;
                }

                list.Add(_operationStepSerializer.Validate(serializationManager, value, dependencies, context));
            }

            return new ValidatedSequenceNode(list);
        }

        public DeserializationResult Read(
            ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context = null)
        {
            var list = ImmutableList.CreateBuilder<OperationStep>();
            var mappings = new List<DeserializationResult>();

            foreach (var dataNode in node.Sequence)
            {
                DeserializationResult result = dataNode is ValueDataNode valueDataNode
                    ? _operationStepSerializer.Read(
                        serializationManager,
                        valueDataNode,
                        dependencies,
                        skipHook,
                        context)
                    : serializationManager.Read(
                        typeof(OperationStep),
                        dataNode,
                        context,
                        skipHook);

                list.Add((OperationStep) result.RawValue!);
                mappings.Add(result);
            }

            return new DeserializedCollection<ImmutableList<OperationStep>, OperationStep>(list.ToImmutable(), mappings,
                ImmutableList.CreateRange);
        }

        public DataNode Write(
            ISerializationManager serializationManager,
            ImmutableList<OperationStep> value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var list = new List<DataNode>();

            foreach (var str in value)
            {
                list.Add(_operationStepSerializer.Write(serializationManager, str, alwaysWrite, context));
            }

            return new SequenceDataNode(list);
        }

        public ImmutableList<OperationStep> Copy(
            ISerializationManager serializationManager,
            ImmutableList<OperationStep> source,
            ImmutableList<OperationStep> target,
            bool skipHook,
            ISerializationContext? context = null)
        {
            return source;
        }
    }
}
