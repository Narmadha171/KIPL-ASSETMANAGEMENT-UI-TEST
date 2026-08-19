using KIPL.AssetManagement.Domain.Enums;

namespace KIPL.AssetManagement.Application.ServiceRequests;

public record ServiceRequestDto(
    int Id, string Reference, string AssetName, string AssetTag,
    string ReporterName, string IssueType, string Description,
    ServiceUrgency Urgency, ServiceRequestStatus Status,
    DateTime RaisedUtc, DateTime? ResolvedUtc, string? Resolution);

public class ReportIssueCommand
{
    public int AssetId { get; set; }
    public int ReportedById { get; set; }
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ServiceUrgency Urgency { get; set; } = ServiceUrgency.Minor;
}
