using System.Text.RegularExpressions;

namespace Concilia.Core.Mockup;

// A problem of the joined prototype and the part that must fix it: the shell
// ("app") or a screen ("P05").
public record MockupProblem(string Target, string Message);

// What does not fit between the parts of the prototype, checked by the
// platform once all the screens are written (prompts v0.8). Until v0.7 a model
// read the whole prototype to find it, and with 19 screens it did not fit in
// 128k (15-10-2026). These are the problems that break a page joined from
// parts written apart:
//   ID-DUP     an element id that another part already uses: getElementById
//              finds the first one, in another screen;
//   JS-GLOBAL  a global name (function, const, let, var, class) that another
//              part already declares: a repeated const or let stops the whole
//              script with a SyntaxError, a repeated function replaces the
//              other one;
//   NAV        mostrarPantalla('P99') to a screen that is not in the map;
//   NAV-FALTA  a link of the map (prompts v0.9: "desde") that the screen it
//              starts from does not have: no action calls mostrarPantalla
//              with the screen it leads to, so nobody gets there.
// The part that comes first (the shell, then the screens in the order of the
// map) keeps the name; the later one must change it.
public static class MockupCoherence
{
    public const string Shell = "app";

    // id="…" as an attribute (not data-id="…"); values built in JavaScript
    // ("${…}", "…" + …) are left out.
    private static readonly Regex ElementId = new("""(?<![\w-])id\s*=\s*["']([^"'${}+]+)["']""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Script = new(@"<script\b[^>]*>([\s\S]*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Navigation = new("""mostrarPantalla\(\s*["'](P\d+)["']\s*\)""", RegexOptions.Compiled);

    // A declaration at the start of a statement: "function name", "async
    // function name", "const name", "let name", "var name", "class name".
    private static readonly Regex Declaration = new(@"\G(?:async\s+)?function\s*\*?\s*([A-Za-z_$][\w$]*)|\G(?:const|let|var|class)\s+([A-Za-z_$][\w$]*)", RegexOptions.Compiled);

    // links: the links of the map (from a screen to another one, with its title).
    public static IReadOnlyList<MockupProblem> Check(string app, IReadOnlyList<(string Id, string Html)> screens,
        IEnumerable<(string From, string To, string Title)>? links = null)
    {
        var parts = new List<(string Target, string Html)> { (Shell, app) };
        parts.AddRange(screens);
        var screenIds = screens.Select(s => s.Id).ToHashSet();
        var problems = new List<MockupProblem>();

        var idOwners = new Dictionary<string, string>();
        var nameOwners = new Dictionary<string, string>();
        foreach (var (target, html) in parts)
        {
            foreach (var id in ElementId.Matches(html).Select(m => m.Groups[1].Value.Trim()).Distinct())
            {
                if (idOwners.TryGetValue(id, out var owner))
                    problems.Add(new(target, $"{Name(target)} ID-DUP: el id \"{id}\" ya lo usa {Owner(owner)}. Cámbialo en esta pantalla, también donde lo use su script."));
                else
                    idOwners[id] = target;
            }

            var globals = Script.Matches(html).SelectMany(m => GlobalNames(m.Groups[1].Value)).Distinct();
            foreach (var name in globals)
            {
                if (nameOwners.TryGetValue(name, out var owner))
                    problems.Add(new(target, $"{Name(target)} JS-GLOBAL: «{name}» ya se declara como global en {Owner(owner)}; dos declaraciones globales con el mismo nombre chocan o se pisan. Renómbrala con el prefijo de la pantalla ({target}_…) o métela dentro de una función."));
                else
                    nameOwners[name] = target;
            }

            foreach (var screen in Navigation.Matches(html).Select(m => m.Groups[1].Value).Distinct().Where(s => !screenIds.Contains(s)))
                problems.Add(new(target, $"{Name(target)} NAV: mostrarPantalla('{screen}') lleva a una pantalla que no está en el mapa."));
        }

        var htmlOf = parts.ToDictionary(p => p.Target, p => p.Html);
        foreach (var (from, to, title) in links ?? [])
        {
            if (!htmlOf.TryGetValue(from, out var html))
                continue;
            var targets = Navigation.Matches(html).Select(m => m.Groups[1].Value).ToHashSet();
            if (!targets.Contains(to))
                problems.Add(new(from, $"{Name(from)} NAV-FALTA: según el mapa, desde esta pantalla se llega a {to} («{title}»), pero ninguna acción llama a mostrarPantalla('{to}'). Añade el botón, enlace o fila que lleva allí."));
        }
        return problems;
    }

    private static string Name(string target) => target == Shell ? "app.html" : target;

    private static string Owner(string target) => target == Shell ? "el armazón (app.html)" : $"la pantalla {target}";

    // The names a script declares at its top level, the ones that are global.
    // A small scanner: it counts braces outside strings and comments, and at
    // depth 0 looks for a declaration at the start of each word. Regular
    // expressions with quotes or braces inside could confuse it; a model's
    // mockup rarely has them at the top level.
    public static IEnumerable<string> GlobalNames(string script)
    {
        var depth = 0;
        for (var i = 0; i < script.Length; i++)
        {
            var c = script[i];
            var next = i + 1 < script.Length ? script[i + 1] : '\0';
            if (c == '/' && next == '/')
            {
                i = script.IndexOf('\n', i) is var end && end < 0 ? script.Length : end;
                continue;
            }
            if (c == '/' && next == '*')
            {
                i = script.IndexOf("*/", i + 2, StringComparison.Ordinal) is var end && end < 0 ? script.Length : end + 1;
                continue;
            }
            if (c is '"' or '\'' or '`')
            {
                i = EndOfString(script, i);
                continue;
            }
            if (c is '{' or '(' or '[')
                depth++;
            else if (c is '}' or ')' or ']')
                depth = Math.Max(0, depth - 1);
            else if (depth == 0 && (i == 0 || !IsNamePart(script[i - 1])))
            {
                var match = Declaration.Match(script, i);
                if (match.Success)
                {
                    yield return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                    i += match.Length - 1;
                }
            }
        }
    }

    // The position of the quote that closes the string opened at start.
    private static int EndOfString(string script, int start)
    {
        var quote = script[start];
        for (var i = start + 1; i < script.Length; i++)
        {
            if (script[i] == '\\')
                i++;
            else if (script[i] == quote)
                return i;
        }
        return script.Length;
    }

    private static bool IsNamePart(char c) => char.IsLetterOrDigit(c) || c is '_' or '$' or '.';
}
