using System.Threading.Channels;

namespace ReqArbiter.Api.Runs;

// Jobs waiting to be executed. The POST endpoint writes the id of a new job
// and answers at once (202); JobWorker reads the ids in the background.
public class JobQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public void Enqueue(Guid jobId) => _channel.Writer.TryWrite(jobId);

    public ChannelReader<Guid> Reader => _channel.Reader;
}
