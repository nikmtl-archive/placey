using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using placey_be.Extensions;
using placey_be.Models.DTOs.Messages;
using placey_be.Services;
using placey_be.Services.Interfaces;

namespace placey_be.Controllers;

[ApiController]
[Route("messages")]
[Authorize]
public class MessagesController(IMessageService messageService, SseHub sseHub) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetConversations()
    {
        var result = await messageService.GetConversationsAsync(User.GetUserId());
        return Ok(result);
    }

    [HttpPost("conversations")]
    public async Task<IActionResult> GetOrCreate([FromBody] GetOrCreateConversationRequest request)
    {
        var result = await messageService.GetOrCreateConversationAsync(User.GetUserId(), request.OtherUserId);
        return Ok(result);
    }

    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> GetConversation(Guid id)
    {
        var result = await messageService.GetConversationAsync(id, User.GetUserId());
        return Ok(result);
    }

    [HttpPost("conversations/{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequest request)
    {
        var result = await messageService.SendMessageAsync(id, User.GetUserId(), request);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await messageService.GetUnreadCountAsync(User.GetUserId());
        return Ok(new { count });
    }

    [HttpGet("stream")]
    public async Task Stream(CancellationToken ct)
    {
        var userId = User.GetUserId();
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        var reader = sseHub.Subscribe(userId);
        try
        {
            await foreach (var msg in reader.ReadAllAsync(ct))
            {
                var json = JsonSerializer.Serialize(msg, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await Response.WriteAsync($"data: {json}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            sseHub.Unsubscribe(userId);
        }
    }
}
