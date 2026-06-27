using placey_be.Models.DTOs.Messages;

namespace placey_be.Services.Interfaces;

public interface IMessageService
{
    Task<ConversationSummaryResponse> GetOrCreateConversationAsync(
        Guid requesterId,
        Guid otherUserId
    );
    Task<List<ConversationSummaryResponse>> GetConversationsAsync(Guid requesterId);
    Task<ConversationDetailResponse> GetConversationAsync(Guid conversationId, Guid requesterId);
    Task<MessageResponse> SendMessageAsync(
        Guid conversationId,
        Guid senderId,
        SendMessageRequest request
    );
    Task<int> GetUnreadCountAsync(Guid userId);
}
