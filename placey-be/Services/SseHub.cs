using System.Collections.Concurrent;
using System.Threading.Channels;
using placey_be.Models.DTOs.Messages;

namespace placey_be.Services;

public class SseHub
{
    private readonly ConcurrentDictionary<Guid, Channel<MessageResponse>> _channels = new();

    public ChannelReader<MessageResponse> Subscribe(Guid userId)
    {
        var ch = _channels.GetOrAdd(userId, _ => Channel.CreateUnbounded<MessageResponse>());
        return ch.Reader;
    }

    public void Unsubscribe(Guid userId) => _channels.TryRemove(userId, out _);

    public void Push(Guid recipientId, MessageResponse msg)
    {
        if (_channels.TryGetValue(recipientId, out var ch))
            ch.Writer.TryWrite(msg);
    }
}
