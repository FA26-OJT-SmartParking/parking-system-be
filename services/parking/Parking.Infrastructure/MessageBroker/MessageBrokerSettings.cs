namespace Parking.Infrastructure.MessageBroker;

/// <summary>RabbitMQ connection, read from the MessageBrokerSettings configuration section.</summary>
public sealed class MessageBrokerSettings
{
    public string HostName { get; set; } = string.Empty;

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string VirtualHost { get; set; } = "/";
}
