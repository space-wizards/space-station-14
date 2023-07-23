using System.Collections;
using System.Linq;

namespace Content.Server.NewCon;

public sealed partial class NewConManager
{
    public string PrettyPrintType(object? value)
    {
        if (value is null)
            return "null";

        if (value is EntityUid uid)
        {
            return _entity.ToPrettyString(uid);
        }

        if (value is Type t)
        {
            return t.PrettyName();
        }

        if (value.GetType().IsAssignableTo(typeof(IEnumerable<EntityUid>)))
        {
            return string.Join(", ", ((IEnumerable<EntityUid>) value).Select(_entity.ToPrettyString));
        }

        if (value.GetType().IsAssignableTo(typeof(IEnumerable)))
        {
            return string.Join(", ", ((IEnumerable) value).Cast<object?>().Select(PrettyPrintType));
        }

        if (value.GetType().IsAssignableTo(typeof(IDictionary)))
        {
            var dict = ((IDictionary) value).GetEnumerator();

            var kvList = new List<string>();

            do
            {
                kvList.Add($"({PrettyPrintType(dict.Key)}, {PrettyPrintType(dict.Value)}");
            } while (dict.MoveNext());

            return $"Dictionary {{{string.Join(", ", kvList)}}}";
        }

        return value.ToString() ?? "[unrepresentable]";
    }
}
