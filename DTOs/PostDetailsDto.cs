namespace Waster.DTOs
{
    public class PostDetailsDto : PostDto
    {
        public string ImageData { get; set; }
        public bool IsOwner { get; set; }
        public UserInfoDto Owner { get; set; }
        public List<ClaimInfoDto> Claims { get; set; }

    }
}
