using Flickoo.Api.ValueObjects;

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
        public required Price Price { get; set; }
        public long LocationId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public required Location Location { get; set; }

        public long UserId { get; set; }
        public required User User { get; set; }

        public long CategoryId { get; set; }
        public required Category Category { get; set; }

        public List<Media> ProductMedias { get; set; } = [];

        public List<Favourite> Favourites { get; set; } = [];
    }
}
