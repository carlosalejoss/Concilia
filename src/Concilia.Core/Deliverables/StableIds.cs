using System.Globalization;

namespace Concilia.Core.Deliverables;

// Stable ids in the working copy of the catalog and the backlog (milestone
// 22, step 110). When the user saves, an element keeps its id if it already
// had it in the draft being replaced (with the prefix of its kind, and only
// once); anything else (new, copied, or a requirement whose type changed)
// takes the next number of its prefix. Deleting leaves a gap, and a number
// already given in the history of the draft is never given again, so that
// "RF-012" always means the same requirement. Replaces "the platform
// renumbers after each edit" (steps 51 and 55); generating from scratch still
// numbers consecutively (Renumber).
public sealed class StableIds
{
    private readonly HashSet<string> _kept;
    private readonly HashSet<string> _used;
    private readonly HashSet<string> _given = [];
    private readonly Dictionary<string, int> _last = [];

    // kept: the ids of the draft being replaced; used: every id given in the
    // history of this draft (the versions of the same job), never reused.
    public StableIds(IEnumerable<string> kept, IEnumerable<string> used)
    {
        _kept = [.. kept];
        _used = [.. used, .. _kept];
    }

    // The id of an element with this prefix ("RF", "HU", "CA"…) and this
    // width of number (RF-001: 3; EP-01: 2).
    public string Assign(string? id, string prefix, int digits)
    {
        if (id is not null && _kept.Contains(id) && NumberOf(id, prefix) is not null && _given.Add(id))
            return id;
        var next = $"{prefix}-{(Last(prefix) + 1).ToString().PadLeft(digits, '0')}";
        _last[prefix] = Last(prefix) + 1;
        _given.Add(next);
        return next;
    }

    // The highest number of the prefix given so far, in the history or in this save.
    private int Last(string prefix)
    {
        if (!_last.TryGetValue(prefix, out var last))
        {
            last = _used.Select(id => NumberOf(id, prefix) ?? 0).DefaultIfEmpty(0).Max();
            _last[prefix] = last;
        }
        return last;
    }

    // 12 for "RF-012" and the prefix "RF"; null if the id is not of the prefix.
    private static int? NumberOf(string id, string prefix) =>
        id.StartsWith(prefix + "-", StringComparison.Ordinal) && int.TryParse(id.AsSpan(prefix.Length + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n > 0
            ? n
            : null;
}
