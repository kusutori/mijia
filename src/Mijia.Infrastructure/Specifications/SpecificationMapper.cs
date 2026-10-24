using System.Collections.Immutable;
using System.Text.Json;
using Mijia.Core.Models;
using Mijia.Infrastructure.Serialization;

namespace Mijia.Infrastructure.Specifications;

internal static class SpecificationMapper
{
    public static DeviceSpecification Map(SpecificationDocument document, string requestedType)
    {
        if (!string.Equals(document.Type, requestedType, StringComparison.Ordinal))
            throw new JsonException("The response specification type does not match the requested type.");

        RequireText(document.Description);
        RequireCollection(document.Services);
        RequireUniquePositiveIds(document.Services.Select(service => service.InstanceId));

        return new DeviceSpecification(document.Type, document.Description,
            document.Services.Select(MapService).ToImmutableArray());
    }

    private static ServiceSpecification MapService(ServiceDocument service)
    {
        RequireText(service.Type);
        RequireText(service.Description);
        RequireCollection(service.Properties);
        RequireCollection(service.Actions);
        RequireCollection(service.Events);
        RequireUniquePositiveIds(service.Properties.Select(property => property.InstanceId));
        RequireUniquePositiveIds(service.Actions.Select(action => action.InstanceId));
        RequireUniquePositiveIds(service.Events.Select(item => item.InstanceId));
        var propertyIds = service.Properties.Select(property => property.InstanceId).ToHashSet();

        foreach (var action in service.Actions)
        {
            RequireText(action.Type);
            RequireText(action.Description);
            RequireReferences(action.Inputs, propertyIds);
            RequireReferences(action.Outputs, propertyIds);
        }

        foreach (var item in service.Events)
        {
            RequireText(item.Type);
            RequireText(item.Description);
            RequireReferences(item.Arguments, propertyIds);
        }

        return new ServiceSpecification(service.InstanceId, service.Type, service.Description,
            service.Properties.Select(property => MapProperty(service.InstanceId, property)).ToImmutableArray(),
            service.Actions.Select(action => new ActionSpecification(
                new ActionId(service.InstanceId, action.InstanceId), action.Type, action.Description,
                [.. action.Inputs], [.. action.Outputs])).ToImmutableArray(),
            service.Events.Select(item => new EventSpecification(item.InstanceId, item.Type,
                item.Description, [.. item.Arguments])).ToImmutableArray());
    }

    private static PropertySpecification MapProperty(int serviceId, PropertyDocument property)
    {
        RequireText(property.Type);
        RequireText(property.Description);
        RequireText(property.Format);
        RequireCollection(property.Access);
        RequireCollection(property.AllowedValues ?? []);
        NumericRange? range = null;
        if (property.Range is { } values)
        {
            if (values.Length != 3 || values[0] > values[1] || values[2] < 0)
                throw new JsonException("A numeric range must contain minimum, maximum and nonnegative step.");
            range = new NumericRange(values[0], values[1], values[2]);
        }

        var access = property.Access.Aggregate(PropertyAccess.None, (current, item) => current | (item switch
        {
            "read" => PropertyAccess.Read,
            "write" => PropertyAccess.Write,
            "notify" => PropertyAccess.Notify,
            _ => PropertyAccess.None
        }));

        return new PropertySpecification(new PropertyId(serviceId, property.InstanceId),
            property.Type, property.Description, property.Format, access, property.Unit, range,
            (property.AllowedValues ?? []).Select(item =>
            {
                RequireText(item.Description);
                var value = item.Value.Deserialize(DeviceValueJson.TypeInfo);
                return new EnumerationValue(value, item.Description);
            }).ToImmutableArray());
    }

    private static void RequireUniquePositiveIds(IEnumerable<int> ids)
    {
        var seen = new HashSet<int>();
        foreach (var id in ids)
            if (id <= 0 || !seen.Add(id))
                throw new JsonException("Instance IDs must be positive and unique within their category.");
    }

    private static void RequireReferences(int[] ids, HashSet<int> available)
    {
        RequireCollection(ids);
        if (ids.Any(id => !available.Contains(id)))
            throw new JsonException("An action or event references a property missing from its service.");
    }

    private static void RequireText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new JsonException("A required specification field is empty.");
    }

    private static void RequireCollection<T>(T[]? values)
    {
        if (values is null || values.Any(value => value is null))
            throw new JsonException("A specification collection cannot be null or contain null elements.");
    }
}
