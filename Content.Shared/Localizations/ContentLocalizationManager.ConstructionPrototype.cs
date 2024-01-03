using System.Collections.Concurrent;

namespace Content.Shared.Localizations
{
    public record ConstructionPrototypeLocalization(
        string? Name,
        string? Description);

    public sealed partial class ContentLocalizationManager
    {
        private readonly ConcurrentDictionary<string, ConstructionPrototypeLocalization>
            _constructionPrototypeLocalizations = new();

        public ConstructionPrototypeLocalization GetConstructionPrototypeLocalization(string prototypeId)
        {
            return _constructionPrototypeLocalizations.GetOrAdd(prototypeId,
                _ => CalcConstructionPrototypeLocalization(prototypeId));
        }

        private ConstructionPrototypeLocalization CalcConstructionPrototypeLocalization(string prototypeId)
        {
            return new ConstructionPrototypeLocalization(
                _loc.TryGetString($"construction-{prototypeId}", out var name) ? name : null,
                _loc.TryGetString($"construction-{prototypeId}.desc", out var desc) ? desc : null);
        }
    }
}
