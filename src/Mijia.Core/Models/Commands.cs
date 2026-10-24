using System.Collections.Immutable;

namespace Mijia.Core.Models;

/// <summary>Accepted means delivery was acknowledged; execution is not yet confirmed.</summary>
public union CommandReceipt(CommandCompleted, CommandAccepted);

public sealed record CommandCompleted;
public sealed record CommandAccepted(string? Message = null);

public sealed record PropertyValue(PropertyId Id, DeviceValue Value);

public sealed record ActionExecution(
    CommandReceipt Receipt,
    ImmutableArray<PropertyValue> Outputs);
