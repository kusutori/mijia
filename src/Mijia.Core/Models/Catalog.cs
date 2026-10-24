using System.Collections.Immutable;

namespace Mijia.Core.Models;

public sealed record DeviceInfo(
    string Id,
    string Name,
    string Model,
    string? SpecificationType,
    string? HomeId = null,
    string? RoomId = null,
    DeviceConnectionState ConnectionState = DeviceConnectionState.Unknown);

public enum DeviceConnectionState
{
    Unknown,
    Online,
    Offline
}

public sealed record HomeInfo(string Id, string Name, ImmutableArray<RoomInfo> Rooms);

public sealed record RoomInfo(string Id, string Name, ImmutableArray<string> DeviceIds);
