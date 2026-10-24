using Mijia.Core.Abstractions;
using Mijia.Core.Models;
using Mijia.Core.Results;

namespace Mijia.Infrastructure.Specifications;

/// <summary>Bounded per-instance cache. Failed requests are never cached.</summary>
public sealed class CachedSpecificationProvider : ISpecificationProvider, IDisposable
{
    private readonly ISpecificationProvider _inner;
    private readonly TimeProvider _time;
    private readonly TimeSpan _lifetime;
    private readonly int _capacity;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public CachedSpecificationProvider(ISpecificationProvider inner, TimeSpan? lifetime = null,
        int capacity = 128, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
        _time = timeProvider ?? TimeProvider.System;
        _lifetime = lifetime ?? TimeSpan.FromHours(6);
        _capacity = capacity;
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_lifetime, TimeSpan.Zero, nameof(lifetime));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
    }

    public async Task<Result<DeviceSpecification>> GetAsync(
        string specificationType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(specificationType))
            return Result.Fail<DeviceSpecification>(new ValidationError("specificationType", "A specification type is required."));

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_entries.TryGetValue(specificationType, out var existing) && existing.ExpiresAt > _time.GetUtcNow())
                return Result.Ok(existing.Specification);

            _entries.Remove(specificationType);
            var result = await _inner.GetAsync(specificationType, cancellationToken).ConfigureAwait(false);
            if (result is Success<DeviceSpecification> success)
            {
                if (_entries.Count >= _capacity)
                    _entries.Remove(_entries.MinBy(pair => pair.Value.ExpiresAt).Key);
                _entries[specificationType] = new Entry(success.Value, _time.GetUtcNow() + _lifetime);
            }
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task InvalidateAsync(string? specificationType = null, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (specificationType is null)
                _entries.Clear();
            else
                _entries.Remove(specificationType);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();

    private sealed record Entry(DeviceSpecification Specification, DateTimeOffset ExpiresAt);
}
