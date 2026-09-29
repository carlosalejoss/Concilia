using System.Threading.Channels;

namespace ReqArbiter.Api.Runs;

// Runs waiting to be executed. The POST endpoint writes the id of a new run
// and answers at once (202); RunWorker reads the ids in the background.
public class RunQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public void Enqueue(Guid runId) => _channel.Writer.TryWrite(runId);

    public ChannelReader<Guid> Reader => _channel.Reader;
}
