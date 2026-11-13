// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace INCIS_SaaP.Hubs;

/// <summary>
/// SignalR Hub για real-time notifications και updates
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Σύνδεση χρήστη
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to notifications", userId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Αποσύνδεση χρήστη
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("sub")?.Value ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} disconnected from notifications", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Αποστολή notification σε συγκεκριμένο χρήστη
    /// </summary>
    public async Task SendToUser(string userId, string message, string type = "info")
    {
        await Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", new
        {
            message,
            type,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Αποστολή notification σε όλους
    /// </summary>
    public async Task Broadcast(string message, string type = "info")
    {
        await Clients.All.SendAsync("ReceiveNotification", new
        {
            message,
            type,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Ειδοποίηση για νέα φόρμουλα
    /// </summary>
    public async Task NotifyNewFormula(string formulaCode, string formulaName)
    {
        await Clients.All.SendAsync("FormulaCreated", new
        {
            code = formulaCode,
            name = formulaName,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Ειδοποίηση για ενημέρωση φόρμουλας
    /// </summary>
    public async Task NotifyFormulaUpdated(int formulaId, string formulaCode)
    {
        await Clients.All.SendAsync("FormulaUpdated", new
        {
            id = formulaId,
            code = formulaCode,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Ειδοποίηση για ολοκλήρωση εισαγωγής
    /// </summary>
    public async Task NotifyImportComplete(string userId, int recordsImported, string fileName)
    {
        await Clients.Group($"user_{userId}").SendAsync("ImportComplete", new
        {
            recordsImported,
            fileName,
            timestamp = DateTime.UtcNow
        });
    }
}
