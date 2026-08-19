using KIPL.AssetManagement.Application.Common.Interfaces;

namespace KIPL.AssetManagement.Infrastructure.Services;

public class DateTimeService : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
}
