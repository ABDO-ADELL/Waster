using Waster.DTOs;
using Waster.Models.DbModels;

namespace Waster.Services
{
    public interface IClaimPostService
    {
        Task<ClaimResponseDto> ClaimPostAsync(Guid postId, string userId);
        Task<ClaimResponseDto> GetClaimDetailsAsync(ClaimPost claim, Post post, string userId);
        Task<List<ClaimPost>> GetUserClaims(string userId, string? status = null);
        Task<List<ClaimResponseDto>>GetPostClaims(string ownerId, Guid postId);


    }
}
