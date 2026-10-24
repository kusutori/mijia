using System.Collections.Immutable;

namespace Mijia.Core.Models;

public readonly record struct PropertyId(int ServiceId, int InstanceId)
{
    public bool IsValid => ServiceId > 0 && InstanceId > 0;
}

public readonly record struct ActionId(int ServiceId, int InstanceId)
{
    public bool IsValid => ServiceId > 0 && InstanceId > 0;
}

[Flags]
public enum PropertyAccess
{
    None = 0,
    Read = 1,
    Write = 2,
    Notify = 4
}

public sealed record NumericRange(decimal Minimum, decimal Maximum, decimal Step);

public sealed record EnumerationValue(DeviceValue Value, string Description);

public sealed record PropertySpecification(
    PropertyId Id,
    string Type,
    string Description,
    string Format,
    PropertyAccess Access,
    string? Unit,
    NumericRange? Range,
    ImmutableArray<EnumerationValue> AllowedValues);

public sealed record ActionSpecification(
    ActionId Id,
    string Type,
    string Description,
    ImmutableArray<int> InputPropertyIds,
    ImmutableArray<int> OutputPropertyIds);

public sealed record EventSpecification(
    int InstanceId,
    string Type,
    string Description,
    ImmutableArray<int> ArgumentPropertyIds);

public sealed record ServiceSpecification(
    int InstanceId,
    string Type,
    string Description,
    ImmutableArray<PropertySpecification> Properties,
    ImmutableArray<ActionSpecification> Actions,
    ImmutableArray<EventSpecification> Events);

public sealed record DeviceSpecification(
    string Type,
    string Description,
    ImmutableArray<ServiceSpecification> Services)
{
    public PropertySpecification? FindProperty(PropertyId id) => Services
        .FirstOrDefault(service => service.InstanceId == id.ServiceId)?
        .Properties.FirstOrDefault(property => property.Id == id);

    public ActionSpecification? FindAction(ActionId id) => Services
        .FirstOrDefault(service => service.InstanceId == id.ServiceId)?
        .Actions.FirstOrDefault(action => action.Id == id);
}
