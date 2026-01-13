using Microsoft.AspNetCore.Http;

namespace GameApi.Models
{
    public class ProfilePictureUploadDto
    {
        public IFormFile File { get; set; } = default!;
    }
}