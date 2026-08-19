namespace KIPL.AssetManagement.Application.Audit;

public record AuditEntryDto(int Id, DateTime TimestampUtc, string UserName, string RoleName, string Action, string Entity, string? Detail);

public class AuditFilter
{
    public string? Search { get; set; }
    public string? Action { get; set; }
    public string? RoleName { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
