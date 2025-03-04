namespace Flickoo.Api.Entities
{
    public class User
    {
        public User()
        {
            CreatedAt = DateTime.UtcNow;
        }
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public long LocationId { get; set; }
        public Location Location { get; set; }

        public List<Product> Products { get; set; } = new List<Product>();

        public List<Like> Likes { get; set; } = new List<Like>();
    }
}
