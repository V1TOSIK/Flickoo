using Microsoft.AspNetCore.Mvc;

namespace Flickoo.Api.DTOs.Media.Create
{
    public class CreateMediaRequest
    {
        public class MediaUploadRequest
        {
            [FromForm(Name = "file")]
            public IFormFile File { get; set; } = null!;

            [FromForm(Name = "fileName")]
            public string FileName { get; set; } = string.Empty;

            [FromForm(Name = "contentType")]
            public string ContentType { get; set; } = string.Empty;
        }
    }
}
