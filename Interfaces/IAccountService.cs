using Microsoft.AspNetCore.Mvc;
using Waster.DTOs;
using static Waster.Services.AccountService;

namespace Waster.Interfaces
{
    public interface IAccountService
    {
        Task<ResponseDto<object>> GetProfileAsync();
        Task<ResponseDto<object>> UpdateNameAsync(UpdateNameRequest request);
        Task<ResponseDto<object>> UpdateBioAsync(UpdateBioRequest request);
        Task<ResponseDto<object>> ChangePassword(ChangePasswordDto dto);
        Task<ResponseDto<object>> ChangeEmail(ChangeEmailDto dto);
        Task<ResponseDto<object>> UpdateLocationAsync(UpdateLocation request);
        Task<ResponseDto<object>> PhoneNumberAsync(PhoneNumberDto request);
        Task<ResponseDto<object>> GetAllPosts(int pageNumber, int pageSize);



    }
}