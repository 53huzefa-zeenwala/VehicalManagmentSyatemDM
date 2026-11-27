using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Paging;
using VehicleManagementDM.Api.Features.Transfers;
using VehicleManagementDM.Api.Features.Trips.Dtos;

namespace VehicleManagementDM.Api.Features.Reports;

/// <summary>Admin dashboard, the five on-screen reports and the audit log (RPT-01..04, AUD-02). Monitoring only (D-021).</summary>
[ApiController]
[Route("api/v1")]
[Authorize(Roles = Roles.Admin)]
public sealed class ReportsController(ReportService reports) : ControllerBase
{
    [HttpGet("dashboard")]
    public Task<DashboardResponse> Dashboard(CancellationToken cancellationToken) =>
        reports.DashboardAsync(cancellationToken);

    /// <summary>R-1 Vehicle running hours.</summary>
    [HttpGet("reports/vehicle-hours")]
    public Task<VehicleHoursReport> VehicleHours([FromQuery] VehicleHoursQuery query, CancellationToken cancellationToken) =>
        reports.VehicleHoursAsync(query, cancellationToken);

    /// <summary>R-2 Staff hours and activity (= driver hours).</summary>
    [HttpGet("reports/staff-hours")]
    public Task<StaffHoursReport> StaffHours([FromQuery] StaffHoursQuery query, CancellationToken cancellationToken) =>
        reports.StaffHoursAsync(query, cancellationToken);

    /// <summary>R-3 Lot utilization.</summary>
    [HttpGet("reports/lot-utilization")]
    public Task<LotUtilizationReport> LotUtilization([FromQuery] LotUtilizationQuery query, CancellationToken cancellationToken) =>
        reports.LotUtilizationAsync(query, cancellationToken);

    /// <summary>R-4 Trip log.</summary>
    [HttpGet("reports/trip-log")]
    public Task<PagedResult<TripSummaryResponse>> TripLog([FromQuery] TripQuery query, CancellationToken cancellationToken) =>
        reports.TripLogAsync(query, User.GetUserId(), cancellationToken);

    /// <summary>R-5 Transfers.</summary>
    [HttpGet("reports/transfers")]
    public Task<PagedResult<TransferResponse>> Transfers([FromQuery] TransferQuery query, CancellationToken cancellationToken) =>
        reports.TransfersAsync(query, cancellationToken);

    [HttpGet("audit-logs")]
    public Task<PagedResult<AuditLogResponse>> AuditLogs([FromQuery] AuditLogQuery query, CancellationToken cancellationToken) =>
        reports.AuditLogsAsync(query, cancellationToken);
}
