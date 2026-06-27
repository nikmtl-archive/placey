using placey_be.Models.DTOs.Messages;
using placey_be.Models.Entities;
using placey_be.Services;
using placey_be.Tests.Helpers;

namespace placey_be.Tests.Services;

public class MessageServiceTests
{
    private (MessageService svc, User alice, User bob) Setup(string dbName)
    {
        var db = DbHelper.CreateDb(dbName);
        var alice = new User { Name = "Alice", Email = "alice@test.com", PasswordHash = "x" };
        var bob = new User { Name = "Bob", Email = "bob@test.com", PasswordHash = "x" };
        db.Users.AddRange(alice, bob);
        db.SaveChanges();
        return (new MessageService(db, new SseHub()), alice, bob);
    }

    [Fact]
    public async Task GetOrCreate_NewPair_CreatesConversation()
    {
        var (svc, alice, bob) = Setup(nameof(GetOrCreate_NewPair_CreatesConversation));

        var result = await svc.GetOrCreateConversationAsync(alice.Id, bob.Id);

        Assert.Equal(bob.Id, result.OtherUserId);
        Assert.Equal("Bob", result.OtherUserName);
    }

    [Fact]
    public async Task GetOrCreate_ExistingPair_ReturnsExisting()
    {
        var (svc, alice, bob) = Setup(nameof(GetOrCreate_ExistingPair_ReturnsExisting));
        var first = await svc.GetOrCreateConversationAsync(alice.Id, bob.Id);

        var second = await svc.GetOrCreateConversationAsync(alice.Id, bob.Id);

        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task GetOrCreate_ReversedOrder_ReturnsSameConversation()
    {
        var (svc, alice, bob) = Setup(nameof(GetOrCreate_ReversedOrder_ReturnsSameConversation));
        var first = await svc.GetOrCreateConversationAsync(alice.Id, bob.Id);

        var second = await svc.GetOrCreateConversationAsync(bob.Id, alice.Id);

        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task GetOrCreate_SelfConversation_Throws()
    {
        var (svc, alice, _) = Setup(nameof(GetOrCreate_SelfConversation_Throws));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.GetOrCreateConversationAsync(alice.Id, alice.Id));
    }

    [Fact]
    public async Task GetOrCreate_UnknownOtherUser_Throws()
    {
        var (svc, alice, _) = Setup(nameof(GetOrCreate_UnknownOtherUser_Throws));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.GetOrCreateConversationAsync(alice.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetConversations_NoConversations_ReturnsEmpty()
    {
        var (svc, alice, _) = Setup(nameof(GetConversations_NoConversations_ReturnsEmpty));

        var result = await svc.GetConversationsAsync(alice.Id);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetConversations_AfterCreate_ReturnsList()
    {
        var (svc, alice, bob) = Setup(nameof(GetConversations_AfterCreate_ReturnsList));
        await svc.GetOrCreateConversationAsync(alice.Id, bob.Id);

        var result = await svc.GetConversationsAsync(alice.Id);

        Assert.Single(result);
        Assert.Equal(bob.Id, result[0].OtherUserId);
    }

    [Fact]
    public async Task SendMessage_ValidConversation_ReturnsMessage()
    {
        var (svc, alice, bob) = Setup(nameof(SendMessage_ValidConversation_ReturnsMessage));
        var conv = await svc.GetOrCreateConversationAsync(alice.Id, bob.Id);

        var msg = await svc.SendMessageAsync(conv.Id, alice.Id, new SendMessageRequest("Hello Bob!"));

        Assert.Equal("Hello Bob!", msg.Body);
        Assert.Equal(alice.Id, msg.SenderId);
    }

    [Fact]
    public async Task SendMessage_ConversationNotFound_Throws()
    {
        var (svc, alice, _) = Setup(nameof(SendMessage_ConversationNotFound_Throws));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.SendMessageAsync(Guid.NewGuid(), alice.Id, new SendMessageRequest("Hi")));
    }

    [Fact]
    public async Task SendMessage_NonMember_Throws()
    {
        var (svc, alice, bob) = Setup(nameof(SendMessage_NonMember_Throws));
        var conv = await svc.GetOrCreateConversationAsync(alice.Id, bob.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.SendMessageAsync(conv.Id, Guid.NewGuid(), new SendMessageRequest("Intruder")));
    }

    [Fact]
    public async Task GetConversation_AsParticipant_ReturnsDetailWithMessages()
    {
        var (svc, alice, bob) = Setup(nameof(GetConversation_AsParticipant_ReturnsDetailWithMessages));
        var conv = await svc.GetOrCreateConversationAsync(alice.Id, bob.Id);
        await svc.SendMessageAsync(conv.Id, alice.Id, new SendMessageRequest("Hey"));

        var detail = await svc.GetConversationAsync(conv.Id, bob.Id);

        Assert.Single(detail.Messages);
        Assert.Equal("Hey", detail.Messages[0].Body);
    }

    [Fact]
    public async Task GetConversation_NonMember_Throws()
    {
        var (svc, alice, bob) = Setup(nameof(GetConversation_NonMember_Throws));
        var conv = await svc.GetOrCreateConversationAsync(alice.Id, bob.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.GetConversationAsync(conv.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetConversation_NotFound_Throws()
    {
        var (svc, alice, _) = Setup(nameof(GetConversation_NotFound_Throws));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.GetConversationAsync(Guid.NewGuid(), alice.Id));
    }

    [Fact]
    public async Task GetConversation_MarksMessagesAsRead()
    {
        var (svc, alice, bob) = Setup(nameof(GetConversation_MarksMessagesAsRead));
        var conv = await svc.GetOrCreateConversationAsync(alice.Id, bob.Id);
        await svc.SendMessageAsync(conv.Id, alice.Id, new SendMessageRequest("Read me"));

        // Bob fetches the conversation — alice's message should be marked read
        var detail = await svc.GetConversationAsync(conv.Id, bob.Id);

        Assert.True(detail.Messages[0].IsRead);
    }

    [Fact]
    public async Task GetUnreadCount_NoMessages_ReturnsZero()
    {
        var (svc, alice, bob) = Setup(nameof(GetUnreadCount_NoMessages_ReturnsZero));
        await svc.GetOrCreateConversationAsync(alice.Id, bob.Id);

        var count = await svc.GetUnreadCountAsync(bob.Id);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetUnreadCount_AfterReceivingMessage_ReturnsOne()
    {
        var (svc, alice, bob) = Setup(nameof(GetUnreadCount_AfterReceivingMessage_ReturnsOne));
        var conv = await svc.GetOrCreateConversationAsync(alice.Id, bob.Id);
        await svc.SendMessageAsync(conv.Id, alice.Id, new SendMessageRequest("Unread"));

        var count = await svc.GetUnreadCountAsync(bob.Id);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetUnreadCount_AfterReadingConversation_ReturnsZero()
    {
        var (svc, alice, bob) = Setup(nameof(GetUnreadCount_AfterReadingConversation_ReturnsZero));
        var conv = await svc.GetOrCreateConversationAsync(alice.Id, bob.Id);
        await svc.SendMessageAsync(conv.Id, alice.Id, new SendMessageRequest("Unread"));
        await svc.GetConversationAsync(conv.Id, bob.Id);

        var count = await svc.GetUnreadCountAsync(bob.Id);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetConversations_ShowsUnreadCountAndLastMessage()
    {
        var (svc, alice, bob) = Setup(nameof(GetConversations_ShowsUnreadCountAndLastMessage));
        var conv = await svc.GetOrCreateConversationAsync(alice.Id, bob.Id);
        await svc.SendMessageAsync(conv.Id, alice.Id, new SendMessageRequest("First"));
        await svc.SendMessageAsync(conv.Id, alice.Id, new SendMessageRequest("Second"));

        var list = await svc.GetConversationsAsync(bob.Id);

        Assert.Equal(2, list[0].UnreadCount);
        Assert.Equal("Second", list[0].LastMessageBody);
    }
}
