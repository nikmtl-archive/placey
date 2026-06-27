using placey_be.Models.DTOs.Messages;
using placey_be.Services.Interfaces;

namespace placey_be.Tests.Helpers;

public class NullMessageService : IMessageService
{
    public Task<ConversationSummaryResponse> GetOrCreateConversationAsync(Guid requesterId, Guid otherUserId) =>
        Task.FromResult(new ConversationSummaryResponse(Guid.NewGuid(), Guid.NewGuid(), "", null, null, null, 0, DateTime.UtcNow));

    public Task<List<ConversationSummaryResponse>> GetConversationsAsync(Guid requesterId) =>
        Task.FromResult(new List<ConversationSummaryResponse>());

    public Task<ConversationDetailResponse> GetConversationAsync(Guid conversationId, Guid requesterId) =>
        throw new NotImplementedException();

    public Task<MessageResponse> SendMessageAsync(Guid conversationId, Guid senderId, SendMessageRequest request) =>
        Task.FromResult(new MessageResponse(Guid.NewGuid(), Guid.NewGuid(), senderId, "", null, "", false, DateTime.UtcNow));

    public Task<int> GetUnreadCountAsync(Guid userId) =>
        Task.FromResult(0);
}
