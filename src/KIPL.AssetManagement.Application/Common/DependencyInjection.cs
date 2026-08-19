using KIPL.AssetManagement.Application.Audit;
using KIPL.AssetManagement.Application.BulkImport;
using KIPL.AssetManagement.Application.Dashboard;
using KIPL.AssetManagement.Application.Inventory;
using KIPL.AssetManagement.Application.Requests;
using KIPL.AssetManagement.Application.Returns;
using KIPL.AssetManagement.Application.ServiceRequests;
using KIPL.AssetManagement.Application.Users;
using KIPL.AssetManagement.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace KIPL.AssetManagement.Application.Common;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IAssetRequestService, AssetRequestService>();
        services.AddScoped<IReturnService, ReturnService>();
        services.AddScoped<IServiceRequestService, ServiceRequestService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IBulkImportService, BulkImportService>();
        services.AddScoped<IPhotoService, PhotoService>();
        services.AddScoped<Notifications.INotificationService, Notifications.NotificationService>();
        return services;
    }
}
