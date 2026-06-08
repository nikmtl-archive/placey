using System.ComponentModel.DataAnnotations;

namespace placey_be.Models.DTOs.Messages;

public record GetOrCreateConversationRequest(
    [Required] Guid OtherUserId
);

public record SendMessageRequest(
    [Required, StringLength(4000, MinimumLength = 1)] string Body
);

public record MessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string SenderName,
    string? SenderAvatarUrl,
    string Body,
    bool IsRead,
    DateTime CreatedAt
);

public record ConversationSummaryResponse(
    Guid Id,
    Guid OtherUserId,
    string OtherUserName,
    string? OtherUserAvatarUrl,
    string? LastMessageBody,
    DateTime? LastMessageAt,
    int UnreadCount,
    DateTime CreatedAt
);

public record ConversationDetailResponse(
    Guid Id,
    Guid OtherUserId,
    string OtherUserName,
    string? OtherUserAvatarUrl,
    List<MessageResponse> Messages
);
