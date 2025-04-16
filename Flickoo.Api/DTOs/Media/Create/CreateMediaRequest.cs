namespace Flickoo.Api.DTOs.Media.Create
{
    public class CreateMediaRequest
    {
        public long ProductId { get; set; }
        public IFormFile File { get; set; }
    }
}
