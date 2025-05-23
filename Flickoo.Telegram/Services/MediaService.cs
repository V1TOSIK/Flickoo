﻿using Flickoo.Telegram.DTOs.Media;
using Flickoo.Telegram.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace Flickoo.Telegram.Services
{
    public class MediaService : IMediaService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MediaService> _logger;
        private readonly string _apiUrl;

        public MediaService(HttpClient httpClient,
            ILogger<MediaService> logger,
            IOptions<ApiOptions> apiOptions)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiUrl = apiOptions.Value.Url;
        }

        public string GetMediaTypeFromMsgAsync(Message msg,
            CancellationToken cancellationToken)
        {
            return msg.Type switch
            {
                MessageType.Photo => "image/jpeg",
                MessageType.Video => "video/mp4",
                MessageType.Audio => "audio/mpeg",
                _ => throw new NotSupportedException($"Message type {msg.Type} is not supported")
            };
        }


        public async Task<Stream> GetMediaFileFromMsgAsync(ITelegramBotClient botClient,
            Message msg,
            long chatId,
            CancellationToken cancellationToken)
        {
            if (msg == null)
            {
                _logger.LogWarning("Message is null");
                throw new ArgumentNullException(nameof(msg));
            }
            if (msg.Type == MessageType.Text)
            {
                _logger.LogWarning("Message type is text");
                throw new NotSupportedException("Text messages are not supported");
            }


            var fileId = msg.Type switch
            {
                MessageType.Photo => msg?.Photo?.Last().FileId,
                MessageType.Video => msg?.Video?.FileId,
                MessageType.Audio => msg?.Audio?.FileId,
                MessageType.Text => msg.Text,
                _ => throw new NotSupportedException($"Message type {msg.Type} is not supported")
            };

            if (string.IsNullOrEmpty(fileId))
            {
                _logger.LogWarning("File ID is null or empty");
                throw new ArgumentNullException(nameof(fileId));
            }

            var file = await botClient.GetFile(fileId, cancellationToken);
            var fileStream = new MemoryStream();
            await botClient.DownloadFile(file.FilePath ?? "", fileStream, cancellationToken);
            fileStream.Position = 0;
            return fileStream;
        }

        public async Task<bool> UploadMediaAsync(ITelegramBotClient botClient,
            MediaRequest mediaRequest,
            long productId,
            CancellationToken cancellationToken)
        {
            if (mediaRequest == null)
            {
                _logger.LogError("mediaRequest is null or empty");
                return false;
            }

            using var content = new MultipartFormDataContent();

            var streamContent = new StreamContent(mediaRequest.FileStream);
            content.Add(streamContent, "file", mediaRequest.FileName);
            content.Add(new StringContent(mediaRequest.FileName), "fileName");
            content.Add(new StringContent(mediaRequest.ContentType), "contentType");

            var response = await _httpClient.PostAsync($"{_apiUrl}/api/Media/{productId}", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var mediaUrl = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation($"Media for product ID {productId} uploaded successfully.");
                return true;
            }
            else
            {
                _logger.LogError($"Failed to upload media for product ID {productId}. Status code: {response.StatusCode}, {response.RequestMessage}");
                return false;
            }
        }


        public async Task<List<IAlbumInputMedia>> GetMediaFromUrlsByProductIdAsync(ITelegramBotClient botClient,
            long productId,
            CancellationToken cancellationToken)
        {
            if (productId < 0)
            {
                _logger.LogWarning("Invalid product ID provided.");
                throw new ArgumentOutOfRangeException(nameof(productId));
            }
            var mediaUrls = await _httpClient.GetFromJsonAsync<List<string>>($"{_apiUrl}/api/Media/{productId}", cancellationToken);

            if (mediaUrls == null || !mediaUrls.Any())
            {
                _logger.LogWarning($"No media found for product ID: {productId}");
                return new List<IAlbumInputMedia>();
            }
            var mediaList = new List<IAlbumInputMedia>();
            foreach (var url in mediaUrls)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url, cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning($"Failed to download media from: {url}");
                        continue;
                    }

                    var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

                    if (contentType.StartsWith("image"))
                    {
                        var inputMedia = new InputMediaPhoto(new InputFileStream(contentStream, Path.GetFileName(url)));
                        mediaList.Add(inputMedia);
                    }
                    else if (contentType.StartsWith("video"))
                    {
                        var inputMedia = new InputMediaVideo(new InputFileStream(contentStream, Path.GetFileName(url)));
                        mediaList.Add(inputMedia);
                    }
                    else
                    {
                        _logger.LogWarning($"Unsupported media type: {contentType}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error loading media from: {url}");
                    return new List<IAlbumInputMedia>();
                }
            }
            return mediaList;
        }
    }
}
