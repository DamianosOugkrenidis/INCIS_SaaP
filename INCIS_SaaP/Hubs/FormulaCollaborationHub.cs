// Author: Damianos Pythagoras Ougkrenidis
// Real-time collaboration hub for formula editing

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace INCIS_SaaP.Hubs;

/// <summary>
/// SignalR Hub for real-time formula collaboration.
/// Users editing the same formula see each other's changes in real-time,
/// with presence awareness and conflict prevention.
/// </summary>
[Authorize]
public class FormulaCollaborationHub : Hub
{
    private static readonly ConcurrentDictionary<string, HashSet<FormulaEditor>> _activeEditors = new();
    private static readonly ConcurrentDictionary<string, string> _connectionFormulaMap = new();
    private readonly ILogger<FormulaCollaborationHub> _logger;

    public FormulaCollaborationHub(ILogger<FormulaCollaborationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Join a formula editing session
    /// </summary>
    public async Task JoinFormula(int formulaId)
    {
        var groupName = $"formula-{formulaId}";
        var editor = new FormulaEditor
        {
            ConnectionId = Context.ConnectionId,
            UserId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown",
            UserName = Context.User?.Identity?.Name ?? "Anonymous",
            JoinedAt = DateTime.UtcNow
        };

        // Track connection → formula mapping
        _connectionFormulaMap[Context.ConnectionId] = groupName;

        // Add to editors set
        _activeEditors.AddOrUpdate(groupName,
            new HashSet<FormulaEditor> { editor },
            (_, existing) => { existing.Add(editor); return existing; });

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        // Notify others that a new editor joined
        await Clients.OthersInGroup(groupName).SendAsync("EditorJoined", new
        {
            editor.UserId,
            editor.UserName,
            editor.JoinedAt,
            FormulaId = formulaId
        });

        // Send current editors list to the new joiner
        if (_activeEditors.TryGetValue(groupName, out var editors))
        {
            await Clients.Caller.SendAsync("CurrentEditors", editors.Select(e => new
            {
                e.UserId,
                e.UserName,
                e.JoinedAt
            }));
        }

        _logger.LogInformation("User {User} joined formula {FormulaId} editing session", editor.UserName, formulaId);
    }

    /// <summary>
    /// Leave a formula editing session
    /// </summary>
    public async Task LeaveFormula(int formulaId)
    {
        var groupName = $"formula-{formulaId}";
        await RemoveFromGroup(groupName);
    }

    /// <summary>
    /// Broadcast a raw material percentage change
    /// </summary>
    public async Task UpdatePercentage(int formulaId, int rawMaterialId, decimal newPercentage)
    {
        var groupName = $"formula-{formulaId}";
        await Clients.OthersInGroup(groupName).SendAsync("PercentageUpdated", new
        {
            FormulaId = formulaId,
            RawMaterialId = rawMaterialId,
            Percentage = newPercentage,
            UpdatedBy = Context.User?.Identity?.Name,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Broadcast a raw material added to formula
    /// </summary>
    public async Task RawMaterialAdded(int formulaId, int rawMaterialId, string rawMaterialName, decimal percentage, string phase)
    {
        var groupName = $"formula-{formulaId}";
        await Clients.OthersInGroup(groupName).SendAsync("RawMaterialAdded", new
        {
            FormulaId = formulaId,
            RawMaterialId = rawMaterialId,
            RawMaterialName = rawMaterialName,
            Percentage = percentage,
            Phase = phase,
            AddedBy = Context.User?.Identity?.Name,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Broadcast a raw material removed from formula
    /// </summary>
    public async Task RawMaterialRemoved(int formulaId, int rawMaterialId)
    {
        var groupName = $"formula-{formulaId}";
        await Clients.OthersInGroup(groupName).SendAsync("RawMaterialRemoved", new
        {
            FormulaId = formulaId,
            RawMaterialId = rawMaterialId,
            RemovedBy = Context.User?.Identity?.Name,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Broadcast formula field update (name, type, notes, etc.)
    /// </summary>
    public async Task FieldUpdated(int formulaId, string fieldName, string newValue)
    {
        var groupName = $"formula-{formulaId}";
        await Clients.OthersInGroup(groupName).SendAsync("FieldUpdated", new
        {
            FormulaId = formulaId,
            FieldName = fieldName,
            NewValue = newValue,
            UpdatedBy = Context.User?.Identity?.Name,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Lock a field to prevent concurrent edits
    /// </summary>
    public async Task LockField(int formulaId, string fieldName)
    {
        var groupName = $"formula-{formulaId}";
        await Clients.OthersInGroup(groupName).SendAsync("FieldLocked", new
        {
            FormulaId = formulaId,
            FieldName = fieldName,
            LockedBy = Context.User?.Identity?.Name,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Unlock a field
    /// </summary>
    public async Task UnlockField(int formulaId, string fieldName)
    {
        var groupName = $"formula-{formulaId}";
        await Clients.OthersInGroup(groupName).SendAsync("FieldUnlocked", new
        {
            FormulaId = formulaId,
            FieldName = fieldName,
            UnlockedBy = Context.User?.Identity?.Name
        });
    }

    /// <summary>
    /// Send typing indicator (cursor position)
    /// </summary>
    public async Task CursorPosition(int formulaId, string fieldName)
    {
        var groupName = $"formula-{formulaId}";
        await Clients.OthersInGroup(groupName).SendAsync("CursorMoved", new
        {
            FieldName = fieldName,
            UserName = Context.User?.Identity?.Name,
            Timestamp = DateTime.UtcNow
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionFormulaMap.TryRemove(Context.ConnectionId, out var groupName))
        {
            await RemoveFromGroup(groupName);
        }
        await base.OnDisconnectedAsync(exception);
    }

    private async Task RemoveFromGroup(string groupName)
    {
        _connectionFormulaMap.TryRemove(Context.ConnectionId, out _);

        if (_activeEditors.TryGetValue(groupName, out var editors))
        {
            var editor = editors.FirstOrDefault(e => e.ConnectionId == Context.ConnectionId);
            if (editor != null)
            {
                editors.Remove(editor);
                if (editors.Count == 0)
                    _activeEditors.TryRemove(groupName, out _);

                await Clients.OthersInGroup(groupName).SendAsync("EditorLeft", new
                {
                    editor.UserId,
                    editor.UserName
                });
            }
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}

/// <summary>
/// Represents a user currently editing a formula
/// </summary>
public class FormulaEditor
{
    public string ConnectionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }

    public override bool Equals(object? obj) => obj is FormulaEditor e && ConnectionId == e.ConnectionId;
    public override int GetHashCode() => ConnectionId.GetHashCode();
}
