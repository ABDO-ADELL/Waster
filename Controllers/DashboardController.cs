using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.AccessControl;
using System.Security.Claims;
using Waster.Interfaces;
using Waster.Models.DTOs;

namespace Waster.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IDashboardService _dashboardService;
        public DashboardController(AppDbContext context ,IMapper mapper,IDashboardService dashboardService)
        {
            _context = context;
            _mapper = mapper;
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> DashboardStats()
        {
            DashboardResponseDto stats = await _dashboardService.DashboardStats();

            if (stats == null)
            {
                return BadRequest(new { message = "No statistics available" });
            }
            return Ok(stats);
        }

        [HttpGet("Calculate")]
        public async Task<IActionResult> GetMyStats()
        {
            DashboardResponseDto stats = await _dashboardService.CalcDashboardStatsAsync();
            if (stats == null)
            {
                return BadRequest(new { message = "No statistics available" });
            }
            return Ok(stats);
        }

    }
}
