#:property PublishAot=false
#:project ../../src/Concilia.Core/Concilia.Core.csproj

// Step 142, real test: a simulated second iteration of the backlog of project 41,
// as a backlog update would leave it, written with the platform's own exporter.
using System.Text.Json.Nodes;
using Concilia.Core.Deliverables.Export;

var dir = args[0];
var generation = Guid.Parse(args[1] + "-0000-0000-0000-000000000000");
var root = JsonNode.Parse(File.ReadAllText(Path.Combine(dir, "backlog41.json")))!.AsObject();

// Iteration 1, unchanged, with the current script: all requirements in iteration 1.
var first = new Dictionary<string, IReadOnlyList<int>>();
foreach (var r in root["epicas"]!.AsArray().SelectMany(e => e!["historias"]!.AsArray()).SelectMany(h => h!["requisitos"]!.AsArray()))
    first[(string)r!] = [1];
File.WriteAllBytes(Path.Combine(dir, "it1b.zip"), BacklogGitHub.Write(
    BacklogExport.Build("ReservaSalas analista", true, DateTimeOffset.Now, DateTimeOffset.Now, root.ToJsonString(), first, "es"), generation));
var epics = root["epicas"]!.AsArray();
JsonObject Story(string id) => epics.SelectMany(e => e!["historias"]!.AsArray()).Select(h => h!.AsObject()).First(h => (string)h["id"]! == id);

// Modified: HU-004 gets a new requirement and criterion.
var hu4 = Story("HU-004");
hu4["requisitos"]!.AsArray().Add("RF-024");
hu4["criterios"]!.AsArray().Add(new JsonObject
{
    ["id"] = "CA-099", ["escenario"] = "Enlace para cancelar en la confirmación",
    ["dado"] = "una reserva confirmada", ["cuando"] = "el sistema envía la confirmación por correo",
    ["entonces"] = "el correo incluye un enlace para cancelar la reserva",
});
hu4["historial"] = new JsonArray(new JsonObject
{
    ["accion"] = "modificada", ["iteraciones"] = new JsonArray(2),
    ["motivo"] = "La iteración 2 pide que la confirmación incluya un enlace para cancelar la reserva.",
});

// Deleted: HU-008 goes to "eliminadas" with its reason.
var ep4 = epics.Select(e => e!.AsObject()).First(e => (string)e["id"]! == "EP-04");
var hu8 = Story("HU-008");
ep4["historias"]!.AsArray().Remove(hu8);
hu8["historial"] = new JsonArray(new JsonObject
{
    ["accion"] = "eliminada", ["iteraciones"] = new JsonArray(2),
    ["motivo"] = "La iteración 2 quita el aviso de día festivo: en modo festivo no se puede reservar.",
});
root["eliminadas"] = new JsonArray(hu8);

// New: HU-013, from the new requirement RF-025.
ep4["historias"]!.AsArray().Add(new JsonObject
{
    ["id"] = "HU-013", ["titulo"] = "Bloquear las reservas en día festivo",
    ["como"] = "empleado", ["quiero"] = "que el sistema no me deje reservar en un día festivo",
    ["para"] = "no ocupar una sala que nadie va a usar",
    ["requisitos"] = new JsonArray("RF-025"),
    ["criterios"] = new JsonArray(new JsonObject
    {
        ["id"] = "CA-100", ["escenario"] = "Reserva en día festivo",
        ["dado"] = "el modo festivo está activado", ["cuando"] = "un empleado solicita una reserva para ese día",
        ["entonces"] = "el sistema la rechaza e indica que es festivo",
    }),
    ["tareas"] = new JsonArray(new JsonObject { ["id"] = "T-050", ["titulo"] = "Rechazar las solicitudes en modo festivo" }),
    ["historial"] = new JsonArray(new JsonObject { ["accion"] = "creada", ["iteraciones"] = new JsonArray(2), ["motivo"] = "Requisito nuevo de la iteración 2." }),
});

// Iterations of the requirements: the two new ones, 2; the rest, 1.
var iterations = new Dictionary<string, IReadOnlyList<int>>();
foreach (var h in epics.SelectMany(e => e!["historias"]!.AsArray()))
    foreach (var r in h!["requisitos"]!.AsArray())
        iterations[(string)r!] = (string)r! is "RF-024" or "RF-025" ? [2] : [1];

var backlog = BacklogExport.Build("ReservaSalas analista", true, DateTimeOffset.Now, DateTimeOffset.Now,
    root.ToJsonString(), iterations, "es");
File.WriteAllBytes(Path.Combine(dir, "it2.zip"), BacklogGitHub.Write(backlog, generation));
Console.WriteLine($"it2.zip, generación {BacklogGitHub.Generation(generation)}");
