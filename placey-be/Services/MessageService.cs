using Microsoft.EntityFrameworkCore;
using placey_be.Data;
using placey_be.Models.DTOs.Messages;
using placey_be.Models.Entities;
using placey_be.Services.Interfaces;

namespace placey_be.Services;

public class MessageService(AppDbContext db, SseHub sseHub) : IMessageService
{
    public async Task<ConversationSummaryResponse> GetOrCreateConversationAsync(
        Guid requesterId,
        Guid otherUserId
    )
    {
        if (requesterId == otherUserId)
            throw new InvalidOperationException("Cannot start a conversation with yourself.");

        if (!await db.Users.AnyAsync(u => u.Id == otherUserId))
            throw new KeyNotFoundException("User not found.");

        var (u1, u2) =
            requesterId < otherUserId ? (requesterId, otherUserId) : (otherUserId, requesterId);

        var conv = await db
            .Conversations.Include(c => c.User1)
            .Include(c => c.User2)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
            .FirstOrDefaultAsync(c => c.User1Id == u1 && c.User2Id == u2);

        if (conv is null)
        {
            conv = new Conversation { User1Id = u1, User2Id = u2 };
            db.Conversations.Add(conv);
            await db.SaveChangesAsync();
            await db.Entry(conv).Reference(c => c.User1).LoadAsync();
            await db.Entry(conv).Reference(c => c.User2).LoadAsync();
        }

        return ToSummary(conv, requesterId);
    }

    public async Task<List<ConversationSummaryResponse>> GetConversationsAsync(Guid requesterId)
    {
        var rows = await db
            .Conversations.Where(c => c.User1Id == requesterId || c.User2Id == requesterId)
            .Select(c => new
            {
                Conv = c,
                User1 = c.User1,
                User2 = c.User2,
                LastMsg = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault(),
                UnreadCount = c.Messages.Count(m => !m.IsRead && m.SenderId != requesterId),
            })
            .OrderByDescending(x => x.LastMsg != null ? x.LastMsg.CreatedAt : x.Conv.CreatedAt)
            .ToListAsync();

        return rows.Select(x =>
            {
                var otherUser = x.Conv.User1Id == requesterId ? x.User2 : x.User1;
                return new ConversationSummaryResponse(
                    x.Conv.Id,
                    otherUser.Id,
                    otherUser.Name,
                    otherUser.AvatarUrl,
                    x.LastMsg?.Body,
                    x.LastMsg?.CreatedAt,
                    x.UnreadCount,
                    x.Conv.CreatedAt
                );
            })
            .ToList();
    }

    public async Task<ConversationDetailResponse> GetConversationAsync(
        Guid conversationId,
        Guid requesterId
    )
    {
        var conv =
            await db
                .Conversations.Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new KeyNotFoundException("Conversation not found.");

        if (conv.User1Id != requesterId && conv.User2Id != requesterId)
            throw new UnauthorizedAccessException("Access denied.");

        var unread = conv.Messages.Where(m => !m.IsRead && m.SenderId != requesterId).ToList();

        if (unread.Count > 0)
        {
            foreach (var m in unread)
                m.IsRead = true;
            await db.SaveChangesAsync();
        }

        var otherUser = conv.User1Id == requesterId ? conv.User2 : conv.User1;

        return new ConversationDetailResponse(
            conv.Id,
            otherUser.Id,
            otherUser.Name,
            otherUser.AvatarUrl,
            conv.Messages.Select(ToMessageResponse).ToList()
        );
    }

    public async Task<MessageResponse> SendMessageAsync(
        Guid conversationId,
        Guid senderId,
        SendMessageRequest request
    )
    {
        var conv =
            await db
                .Conversations.Include(c => c.User1)
                .Include(c => c.User2)
                .FirstOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new KeyNotFoundException("Conversation not found.");

        if (conv.User1Id != senderId && conv.User2Id != senderId)
            throw new UnauthorizedAccessException("Access denied.");

        var msg = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Body = request.Body,
        };

        db.Messages.Add(msg);
        await db.SaveChangesAsync();

        await db.Entry(msg).Reference(m => m.Sender).LoadAsync();

        var response = ToMessageResponse(msg);

        var recipientId = conv.User1Id == senderId ? conv.User2Id : conv.User1Id;
        sseHub.Push(recipientId, response);

        return response;
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await db.Messages.CountAsync(m =>
            !m.IsRead
            && m.SenderId != userId
            && (m.Conversation.User1Id == userId || m.Conversation.User2Id == userId)
        );
    }

    private static MessageResponse ToMessageResponse(Message m) =>
        new(
            m.Id,
            m.ConversationId,
            m.SenderId,
            m.Sender.Name,
            m.Sender.AvatarUrl,
            m.Body,
            m.IsRead,
            m.CreatedAt
        );

    private static ConversationSummaryResponse ToSummary(Conversation conv, Guid requesterId)
    {
        var otherUser = conv.User1Id == requesterId ? conv.User2 : conv.User1;
        var lastMsg = conv.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
        var unread = conv.Messages.Count(m => !m.IsRead && m.SenderId != requesterId);
        return new ConversationSummaryResponse(
            conv.Id,
            otherUser.Id,
            otherUser.Name,
            otherUser.AvatarUrl,
            lastMsg?.Body,
            lastMsg?.CreatedAt,
            unread,
            conv.CreatedAt
        );
    }
}
