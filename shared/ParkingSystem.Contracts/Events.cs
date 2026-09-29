namespace ParkingSystem.Contracts;

// Events exchanged between services through RabbitMQ (see docs/huong-dan-setup-microservices.md, section 4).
// MassTransit publishes each type to an exchange named "ParkingSystem.Contracts:<TypeName>";
// the Python AI service binds to those names, so renaming a type or this namespace is a breaking change.
// Money is in VND (long), time is UTC.

/// <summary>identity → notification. Contains the OTP code: never log the message body.</summary>
public record OtpRequested(Guid UserId, string Email, string Code, DateTimeOffset ExpiresAt);

/// <summary>parking → booking, ai.</summary>
public record LotApproved(Guid LotId);

/// <summary>parking → booking, ai. Opening hours, capacity, pricing or reservation ratio changed.</summary>
public record LotUpdated(Guid LotId);

/// <summary>parking → ai.</summary>
public record SlotStatusChanged(Guid LotId, string SlotCode, string Status, DateTimeOffset At);

/// <summary>parking → ai. Occupancy of a motorbike/bicycle zone (BR-17).</summary>
public record ZoneOccupancyChanged(Guid LotId, string ZoneCode, int Occupied, int Capacity, DateTimeOffset At);

/// <summary>booking → payment, ai.</summary>
public record ReservationCreated(Guid ReservationId, Guid LotId, Guid UserId, long DepositAmount, DateTimeOffset DepositDeadline);

/// <summary>booking → payment, notification, ai.</summary>
public record ReservationConfirmed(Guid ReservationId);

/// <summary>booking → payment, notification, ai.</summary>
public record ReservationCancelled(Guid ReservationId, string Reason);

/// <summary>payment → booking.</summary>
public record DepositPaid(Guid ReservationId, long Amount);

/// <summary>payment → booking.</summary>
public record DepositFailed(Guid ReservationId, string Reason);

/// <summary>booking → parking, payment, ai. Plate is null for bicycles (BR-18).</summary>
public record SessionOpened(Guid SessionId, Guid LotId, string VehicleType, string? Plate, DateTimeOffset EnteredAt);

/// <summary>booking → parking, payment, ai.</summary>
public record SessionClosed(Guid SessionId, Guid LotId, string VehicleType, long Amount, long DepositDeducted, DateTimeOffset ExitedAt);

/// <summary>payment → booking, notification.</summary>
public record PaymentCompleted(Guid SessionId, long Amount, string Method);

/// <summary>payment → notification.</summary>
public record RefundCompleted(Guid ReservationId, long Amount);
