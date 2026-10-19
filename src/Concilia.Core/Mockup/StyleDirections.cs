namespace Concilia.Core.Mockup;

// Step 94 (decision of the author, 19-10-2026): without a style reference
// (.zip), every mockup came out with the same look and organization, the
// "sober and readable" default of the model. Now the platform gives each
// project one of these directions of style, always the same for the same
// project (so resuming a mockup does not change it), and asks the model to
// adapt it to the sector and the users of the system. They are directions,
// not templates: the details are the model's. With a style reference nothing
// changes: the style of the client rules.
public static class StyleDirections
{
    public static readonly IReadOnlyList<string> All =
    [
        "institucional y sobria: navegación lateral oscura, fondo gris muy claro, acento azul petróleo, tipografía sans-serif del sistema, tablas densas y esquinas poco redondeadas",
        "cálida y cercana: navegación superior con pestañas, fondo crema, acento terracota, títulos con tipografía serif, tarjetas con esquinas muy redondeadas y mucho espacio",
        "limpia y clínica: blanco dominante, acento verde azulado, iconos de línea fina, navegación lateral clara que se pliega y formularios con la etiqueta encima del campo",
        "moderna y contrastada: modo oscuro, acento violeta, tarjetas con sombra suave, navegación inferior en el móvil y lateral en el escritorio, y cifras grandes en los paneles",
        "editorial: mucho blanco, títulos en serif y texto en sans-serif, acento rojo discreto, navegación superior mínima y listas en lugar de tablas",
        "corporativa y compacta: barra superior azul marino con menús desplegables, mucha información por pantalla, tablas con filtros y acento naranja para las acciones principales",
        "amable y colorida: un color pastel por sección, iconos grandes, botones redondeados y un inicio con tarjetas que llevan a cada sección",
        "técnica: fondo gris pizarra, acento cian, datos en tipografía monoespaciada, paneles divididos y barra lateral con las secciones agrupadas",
    ];

    // The direction of a project: always the same for the same project.
    public static string For(int projectId) => All[projectId % All.Count];

    // What §5 of the mockup prompts says when there is no style reference.
    public static string WithoutReference(string direction) =>
        $"No hay referencia de estilo. La plataforma asigna a este proyecto esta dirección de estilo, para que no todos los mockups se parezcan: **{direction}**. " +
        "Adáptala al sector y a los usuarios del sistema, con una identidad visual propia y no el aspecto por defecto de una plantilla. " +
        "Es una dirección, no una plantilla: los detalles los decides tú, siempre legibles y accesibles.";
}
