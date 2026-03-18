using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using IWOMR.Application.Common.Interfaces;
using IWOMR.Domain.Entities;

namespace IWOMR.API.Hubs;

[Authorize]
public class ChatHub(
    IExchangeRepository exchanges,
    IMessageRepository messages,
    IUnitOfWork uow) : Hub
{
    // ── Connect: join the exchange group ──────────────────────────────────────
    public override async Task OnConnectedAsync()
    {
        var exchangeId = GetExchangeId();
        var userId     = GetUserId();

        if (exchangeId is null || userId is null)
        {
            Context.Abort();
            return;
        }

        var exchange = await exchanges.GetByIdAsync(exchangeId.Value);

        // Only participants of the exchange can join
        if (exchange is null ||
            (exchange.UserAId != userId && exchange.UserBId != userId))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(exchangeId.Value));
        await base.OnConnectedAsync();
    }

    // ── Disconnect: leave the group ───────────────────────────────────────────
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var exchangeId = GetExchangeId();
        if (exchangeId is not null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(exchangeId.Value));

        await base.OnDisconnectedAsync(exception);
    }

    // ── SendMessage: client → hub → group ────────────────────────────────────
    public async Task SendMessage(string body)
    {
        var exchangeId = GetExchangeId();
        var userId     = GetUserId();

        if (exchangeId is null || userId is null || string.IsNullOrWhiteSpace(body))
            return;

        var exchange = await exchanges.GetByIdAsync(exchangeId.Value);
        if (exchange is null || exchange.Status != ExchangeStatus.Accepted)
            return;

        Message message;
        try { message = Message.Create(exchangeId.Value, userId.Value, body); }
        catch { return; }

        await messages.AddAsync(message);
        await uow.SaveChangesAsync();

        // Broadcast to all connections in this exchange group
        await Clients.Group(GroupName(exchangeId.Value))
            .SendAsync("ReceiveMessage", new
            {
                id       = message.Id,
                senderId = message.SenderId,
                body     = message.Body,
                sentAt   = message.SentAt
            });
    }

    // ── Typing indicator ──────────────────────────────────────────────────────
    public async Task Typing()
    {
        var exchangeId = GetExchangeId();
        var userId     = GetUserId();
        if (exchangeId is null || userId is null) return;

        await Clients.OthersInGroup(GroupName(exchangeId.Value))
            .SendAsync("UserTyping", userId.Value);
    }

    private static string GroupName(Guid exchangeId) => $"exchange-{exchangeId:N}";

    private Guid? GetExchangeId()
    {
        var raw = Context.GetHttpContext()?.Request.Query["exchangeId"].ToString();
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private Guid? GetUserId()
    {
        var raw = Context.User?.FindFirstValue(
            System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
