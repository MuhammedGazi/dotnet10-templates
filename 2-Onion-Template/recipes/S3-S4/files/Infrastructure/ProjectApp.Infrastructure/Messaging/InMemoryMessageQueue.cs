using System.Threading.Channels;
using ProjectApp.Domain.Events;

namespace ProjectApp.Infrastructure.Messaging;

public class InMemoryMessageQueue
{
    private readonly Channel<IEventOrMessage> _channel = Channel.CreateBounded<IEventOrMessage>(new BoundedChannelOptions(1000)
    {
        SingleReader = true,
        FullMode = BoundedChannelFullMode.Wait // TryWrite kullanıldığı için doluyken yazma beklemez, false döner
    });

    public ChannelWriter<IEventOrMessage> Writer => _channel.Writer;
    public ChannelReader<IEventOrMessage> Reader => _channel.Reader;
}
