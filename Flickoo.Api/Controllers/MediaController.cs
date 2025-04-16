using Flickoo.Api.DTOs.Media.Create;
using Flickoo.Api.DTOs.Media.Update;
using Flickoo.Api.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

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
        /*[HttpPost]
        public async Task<ActionResult<string>> AddMediaFile([FromBody] CreateMediaRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request cannot be null");
            }

            if (request.File == null || request.ProductId < 0)
            {
                return BadRequest("File or Product ID is invalid");
            }

            var mediaUrl = await _mediaService.UploadMediaAsync(request.File.OpenReadStream(), request.File.FileName, request.ProductId);

            if (mediaUrl == null)
            {
                return BadRequest("Failed to upload media");
            }

            return Ok("Media is added");

        }
*/
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
        public async Task<ActionResult<string>> UpdateProductMedias([FromRoute] long productId, [FromBody] IEnumerable<UpdateMediaRequest> requests)
        {
            if (productId < 0)
            {
                return BadRequest("Invalid product ID provided.");
            }

            foreach (var request in requests)
            {
                if (request == null)
                {
                    return BadRequest("Request cannot be null");
                }
                if (request == null)
                {
                    return BadRequest("Request cannot be null");
                }
                if (request.FileStream == null || productId < 0)
                {
                    return BadRequest("File or Product ID is invalid");
                }
                var isUpdatedUrl = await _mediaService.UploadMediaAsync(request.FileStream, request.FileName, request.FileName, productId);
                if (isUpdatedUrl == null)
                {
                    return BadRequest("Failed to update media");
                }
            }
                return Ok("All media is updated");
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
