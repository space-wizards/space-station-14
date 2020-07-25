using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Content.Tools
{
    public class TypeTagPreserver : IEmitter
    {
        private readonly IEmitter _emitter;

        public TypeTagPreserver(IEmitter emitter)
        {
           _emitter = emitter;
        }

        public void Emit(ParsingEvent @event)
        {
            if (@event is MappingStart mapping)
            {
                @event = new MappingStart(mapping.Anchor, mapping.Tag, false, mapping.Style, mapping.Start, mapping.End);
            }

            _emitter.Emit(@event);
        }
    }
}
