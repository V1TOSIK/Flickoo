namespace Flickoo.Api.Entities
{
    public class MediaFile
    {
        public long Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public FileType TypeOfFile { get; set; }

        public long ProductId { get; set; }
        public Product Product { get; set; }
    }
    public enum FileType
    {
        ImageJpeg,
        ImagePng,
        VideoMp4
    }
}
