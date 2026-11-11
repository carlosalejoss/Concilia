using Concilia.Core.Runner;

namespace Concilia.Api.Runs;

// The provider of each analysis role (A, B, arbiter and suggestions), step 128.
// Since 10-11-2026 model B goes through OpenCode Go while the other roles stay
// on the LiteLLM gateway, so one provider for all no longer works.
//
// It is itself the IRunnerProvider the executors receive: each run is sent to
// the provider of its model. A RunRequest carries the model but not the role,
// and the model is enough: every role has its model in the configuration. That
// way the executors do not change, and a new arbitration with the model of B
// (step 136) goes through OpenCode Go too. Two roles may share a model (A and
// the arbiter are both SOL) only if they also share the provider; otherwise the
// API does not start, because the run could not know where to go.
public class RunnerProviders : IRunnerProvider
{
    private readonly IRunnerProvider _default;
    private readonly Dictionary<string, Entry> _byModel = new(StringComparer.Ordinal);

    // A role whose provider cannot run has no provider, only the reason (an
    // OpenCode Go role without its key): its tasks are refused when launched.
    public record Entry(string ProviderName, IRunnerProvider? Provider, string? Error);

    public RunnerProviders(IRunnerProvider defaultProvider, IEnumerable<(string Role, string Model, Entry Entry)> roles)
    {
        _default = defaultProvider;
        foreach (var (role, model, entry) in roles)
        {
            if (_byModel.TryGetValue(model, out var existing) && existing.ProviderName != entry.ProviderName)
            {
                throw new InvalidOperationException(
                    $"El modelo \"{model}\" está en dos papeles con proveedores distintos ({existing.ProviderName} y {entry.ProviderName}, " +
                    $"este en el papel {role}). Usa el mismo proveedor o modelos distintos.");
            }
            _byModel[model] = entry;
        }
    }

    // The name of the default provider (Runner:Provider). Each run stores the
    // provider of its own model instead (NameFor).
    public string Name => _default.Name;

    // The provider each run of this model goes through, stored with the run.
    // A model of no role (it should not happen) uses the default one.
    public string NameFor(string model) => _byModel.TryGetValue(model, out var entry) ? entry.ProviderName : _default.Name;

    // Why a task with this model cannot be launched, or null if it can.
    public string? ErrorFor(string model) => _byModel.TryGetValue(model, out var entry) ? entry.Error : null;

    // The first error among the models of a task (A, B and arbiter of a job).
    public string? ErrorFor(IEnumerable<string> models) => models.Select(ErrorFor).FirstOrDefault(e => e is not null);

    public Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken)
    {
        if (!_byModel.TryGetValue(request.ModelId, out var entry))
        {
            return _default.RunAsync(request, onEvent, cancellationToken);
        }
        // The endpoints check ErrorFor before creating the job, so this only
        // happens if the configuration changed while the job waited in the queue.
        if (entry.Provider is null)
        {
            return Task.FromException<RunResult>(new InvalidOperationException(entry.Error));
        }
        return entry.Provider.RunAsync(request, onEvent, cancellationToken);
    }
}
