using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Body.Surgery.Operation.Step.Serializers
{
    public class OperationStepSerializer :
        ITypeSerializer<OperationStep, ValueDataNode>,
        ITypeSerializer<OperationStep, MappingDataNode>
    {
        public DataNode Write(
            ISerializationManager serializationManager,
            OperationStep value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return value.Conditional == null
                ? new ValueDataNode(value.Id)
                : serializationManager.WriteValue(value, alwaysWrite, context);
        }

        public ValidationNode Validate(
            ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            return string.IsNullOrEmpty(node.Value)
                ? new ErrorNode(node, "No id found in value data node")
                : new ValidatedValueNode(node);
        }

        public DeserializationResult Read(
            ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context = null)
        {
            var mapping = new MappingDataNode();
            mapping.Add("id", node);

            return serializationManager.Read(typeof(OperationStep), mapping, context, skipHook);
        }

        public OperationStep Copy(
            ISerializationManager serializationManager,
            OperationStep source,
            OperationStep target,
            bool skipHook,
            ISerializationContext? context = null)
        {
            return new()
            {
                Id = source.Id,
                Conditional = source.Conditional
            };
        }

        public ValidationNode Validate(
            ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            return serializationManager.ValidateNode<OperationStep>(node, context);
        }

        public DeserializationResult Read(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null)
        {
            return serializationManager.Read(typeof(OperationStep), node, context);
        }
    }
}
