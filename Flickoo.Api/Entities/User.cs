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
        public string Nickname { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool Registered { get; set; }

        public long LocationId { get; set; }
        public Location? Location { get; set; }

        public List<Product> Products { get; set; } = [];

        public List<Favourite> Favourites { get; set; } = [];
    }
}
