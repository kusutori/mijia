using Mijia.Core.Models;
using Mijia.Core.Results;

namespace Mijia.Core.Services;

public interface IPropertyValueValidator
{
    Result<Unit> Validate(PropertySpecification property, DeviceValue value);
}
