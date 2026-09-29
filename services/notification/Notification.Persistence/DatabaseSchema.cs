namespace Notification.Persistence;

/// <summary>All services share one database; this service keeps its tables, outbox tables and migration history in this schema.</summary>
public static class DatabaseSchema
{
    public const string Name = "notification";
}
