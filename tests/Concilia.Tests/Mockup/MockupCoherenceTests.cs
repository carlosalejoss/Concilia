using Concilia.Core.Mockup;

namespace Concilia.Tests.Mockup;

// What the platform checks between the parts of a prototype (prompts v0.8).
public class MockupCoherenceTests
{
    private const string App = """
        <!doctype html><html><body><nav><a id="menu" onclick="mostrarPantalla('P01')">Inicio</a></nav>
        <script>
        const DATOS = { reservas: [] };
        function mostrarPantalla(id) {}
        </script>
        <!-- PANTALLAS --></body></html>
        """;

    [Fact]
    public void Parts_that_fit_have_no_problems()
    {
        var problems = MockupCoherence.Check(App, [
            ("P01", "<section data-pantalla=\"P01\"><div id=\"P01-lista\"></div><script>function P01_pintar() { const x = 1; }</script></section>"),
            ("P02", "<section data-pantalla=\"P02\"><div id=\"P02-lista\" data-id=\"menu\"></div><script>function P02_pintar() { let x = 2; }</script></section>"),
        ]);

        Assert.Empty(problems);
    }

    [Fact]
    public void A_repeated_id_or_global_name_is_a_problem_of_the_later_part()
    {
        var problems = MockupCoherence.Check(App, [
            ("P01", "<section data-pantalla=\"P01\"><div id=\"lista\"></div><script>function guardar() {}\nconst DATOS = {};</script></section>"),
            ("P02", "<section data-pantalla=\"P02\"><div id=\"lista\"></div><script>async function guardar() {}</script></section>"),
        ]);

        Assert.Equal(
        [
            "P01 JS-GLOBAL: «DATOS» ya se declara como global en el armazón (app.html); dos declaraciones globales con el mismo nombre chocan o se pisan. Renómbrala con el prefijo de la pantalla (P01_…) o métela dentro de una función.",
            "P02 ID-DUP: el id \"lista\" ya lo usa la pantalla P01. Cámbialo en esta pantalla, también donde lo use su script.",
            "P02 JS-GLOBAL: «guardar» ya se declara como global en la pantalla P01; dos declaraciones globales con el mismo nombre chocan o se pisan. Renómbrala con el prefijo de la pantalla (P02_…) o métela dentro de una función.",
        ], problems.Select(p => p.Message));
        Assert.Equal(["P01", "P02", "P02"], problems.Select(p => p.Target));
    }

    [Fact]
    public void A_link_to_a_screen_that_is_not_in_the_map_is_a_problem()
    {
        var problems = MockupCoherence.Check(App, [("P01", "<section data-pantalla=\"P01\"><button onclick=\"mostrarPantalla('P07')\">Ir</button></section>")]);

        Assert.Equal("P01 NAV: mostrarPantalla('P07') lleva a una pantalla que no está en el mapa.", Assert.Single(problems).Message);
    }

    [Fact]
    public void Only_top_level_declarations_are_global()
    {
        const string script = """
            // function comentada() {}
            /* const tampocoEsta = 1; */
            const texto = "function enTexto() { const no = 1; }";
            function fuera() { function dentro() {} const local = 1; }
            if (true) { let bloque = 2; }
            for (let i = 0; i < 2; i++) {}
            class Pantalla {}
            var lista = [1, 2], otra = 3;
            const plantilla = `${texto} { let tampoco = 1; }`;
            """;

        Assert.Equal(["texto", "fuera", "Pantalla", "lista", "plantilla"], MockupCoherence.GlobalNames(script));
    }
}
