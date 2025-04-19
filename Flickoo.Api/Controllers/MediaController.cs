using Flickoo.Api.DTOs.Media.Create;
using Flickoo.Api.DTOs.Media.Update;
using Flickoo.Api.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using static Flickoo.Api.DTOs.Media.Create.CreateMediaRequest;

namespace Flickoo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _mediaService;
        public MediaController(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        // GET
        #region GET
        [HttpGet("{productId}")]
        public async Task<ActionResult<IEnumerable<string>>> GetAllProductMediaUrls([FromRoute] long productId)
        {
            if (productId < 0)
            {
                return BadRequest("Invalid product ID provided.");
            }
            var urls = await _mediaService.GetMediaUrlsAsync(productId);
            if (urls == null || !urls.Any())
            {
                return NotFound("Urls not fount");
            }
            return Ok(urls);
        }
        #endregion

        //POST
        #region POST
        [HttpPost("{productId}")]
        public async Task<ActionResult<string>> AddMediaFile(
            [FromRoute] long productId,
            [FromForm] MediaUploadRequest request)
        {
            if (request.File == null || string.IsNullOrEmpty(request.ContentType))
                return BadRequest("File or content type is missing.");

            var mediaUrl = await _mediaService.UploadMediaAsync(
                request.File.OpenReadStream(),
                request.FileName,
                request.ContentType,
                productId);

            if (mediaUrl == null)
            {
                return BadRequest("Failed to upload media");
            }

            return Ok("Media is added");
        }

        [HttpPost("{productId}/multiple")]
        public async Task<ActionResult<string>> AddMultipleMediaFiles(
            [FromRoute] long productId,
            [FromForm] List<IFormFile> files)
        {
            if (files == null || !files.Any())
            {
                return BadRequest("Requests cannot be null or empty");
            }

            foreach (var file in files)
            {
                if (file == null)
                {
                    return BadRequest("Request cannot be null");
                }

                if (file == null || productId < 0)
                {
                    return BadRequest("File or Product ID is invalid");
                }

                var mediaUrl = await _mediaService.UploadMediaAsync(file.OpenReadStream(), file.FileName, file.ContentType, productId);

                if (mediaUrl == null)
                {
                    return BadRequest("Failed to upload media");
                }
            }
            return Ok("All media is added");
        }


        #endregion

        // PUT
        #region PUT
        [HttpPut("{productId}")]
        public async Task<ActionResult<string>> UpdateProductMedia(
            [FromRoute] long productId,
            [FromForm] MediaUploadRequest request)
        {
            if (request.File == null || string.IsNullOrEmpty(request.ContentType))
                return BadRequest("File or content type is missing.");

            var mediaUrl = await _mediaService.UploadMediaAsync(
                request.File.OpenReadStream(),
                request.FileName,
                request.ContentType,
                productId);

            if (mediaUrl == null)
            {
                return BadRequest("Failed to update media");
            }

            return Ok("Media is updated");
        }
        #endregion

        // DELETE
        #region DELETE
        [HttpDelete("{productId}")]
        public async Task<ActionResult<string>> Delete([FromRoute] long productId)
        {
            if (productId < 0)
            {
                return BadRequest("Invalid product ID provided.");
            }
            var isDeleted = await _mediaService.DeleteMediaAsync(productId);
            if (!isDeleted)
            {
                return NotFound("Media not found");
            }
            return Ok("Media is deleted");
        }
        #endregion
    }
}
