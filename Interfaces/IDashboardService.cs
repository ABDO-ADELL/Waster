using Waster.Models.DTOs;

namespace Waster.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardResponseDto> CalcDashboardStatsAsync();
        Task<DashboardResponseDto> DashboardStats();

    }
}