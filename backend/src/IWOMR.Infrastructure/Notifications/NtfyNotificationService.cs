using System.Text;
using Microsoft.Extensions.Configuration;
using IWOMR.Application.Common.Interfaces;

namespace IWOMR.Infrastructure.Notifications;

public class NtfyNotificationService(HttpClient http, IConfiguration config) : INotificationService
{
    private readonly string _baseUrl = config["Ntfy:BaseUrl"]
        ?? throw new InvalidOperationException("Ntfy:BaseUrl not configured.");

    public async Task SendAsync(Guid userId, string title, string message, CancellationToken ct = default)
    {
        // Each user subscribes to their own private topic: user-{userId}
        var topic = $"user-{userId:N}";

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/{topic}");
        request.Content = new StringContent(message, Encoding.UTF8, "text/plain");
        request.Headers.Add("X-Title", title);
        request.Headers.Add("X-Priority", "default");

        await http.SendAsync(request, ct);
    }
}
