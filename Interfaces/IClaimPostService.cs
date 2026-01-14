using Microsoft.AspNetCore.Mvc;
using Waster.DTOs;
using Waster.Models.DbModels;

namespace Waster.Interfaces
{
    public interface IClaimPostService
    {
        Task<Result<ClaimResponseDto>> ClaimPostAsync(Guid postId, string userId);
        Task<ClaimResponseDto> GetClaimDetailsAsync(ClaimPost claim, Post post, string userId);
        Task<List<ClaimPost>> GetUserClaims(string userId, string? status = null);
        Task<List<ClaimResponseDto>>GetPostClaims(string ownerId, Guid postId);
        //   Task<bool> UpdateDashboard(string ownerId, string recipientId, double quantity);
        Task<ResponseDto<object>> CancelClaim(Guid claimId);
        Task<ResponseDto<object>> CompleteClaim(Guid claimId);
        Task<ResponseDto<object>> RejectClaim(Guid claimId);
        Task<ResponseDto<object>> ApproveClaim(Guid claimId);




    }
}
