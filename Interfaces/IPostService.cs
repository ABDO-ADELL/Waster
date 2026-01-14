using Waster.DTOs;

namespace Waster.Interfaces
{
    public interface IPostService
    {
        Task<ResponseDto<object>> CreatePost(PostDto model);
        Task<ResponseDto<object>> UpdatePost(Guid id, UpdatePostDto dto);
        Task<ResponseDto<object>> DeletePost(Guid id);


    }
}