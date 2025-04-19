namespace Flickoo.Api.Entities
{
    public class Favourite
    {
        public Favourite()
        {
            CreatedAt = DateTime.UtcNow;
        }
        public long Id { get; set; }
        public long UserId { get; set; }
        public long ProductId { get; set; }
        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
        public Product? Product { get; set; }
    }
}
