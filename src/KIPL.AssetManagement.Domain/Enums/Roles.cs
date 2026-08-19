namespace KIPL.AssetManagement.Domain.Enums;

/// <summary>Canonical role names. These are the strings stored in ASP.NET Identity roles.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string ITAssetManager = "IT Asset Manager";
    public const string ITAssetExecutive = "IT Asset Executive";
    public const string HR = "HR";
    public const string ReportingManager = "Reporting Manager";
    public const string Employee = "Employee";

    public static readonly string[] All =
    {
        Admin, ITAssetManager, ITAssetExecutive, HR, ReportingManager, Employee
    };

    /// <summary>Roles that see the operations shell (dashboard, inventory, audit).</summary>
    public static readonly string[] Operations = { Admin, ITAssetManager, ITAssetExecutive, HR };
}
