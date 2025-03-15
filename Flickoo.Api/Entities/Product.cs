namespace Flickoo.Api.Entities
{
    public class Product
    {
        public Product()
        {
            CreatedAt = DateTime.UtcNow;
        }
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public long UserId { get; set; }
        public User? User { get; set; }

        public long CategoryId { get; set; }
        public Category? Category { get; set; }

        public List<MediaFile> ProductMedias { get; set; } = [];

        public List<Like> Likes { get; set; } = [];
    }
}
