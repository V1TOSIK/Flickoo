namespace Flickoo.Api.Entities
{
    public class Like
    {
        public Like()
        {
            CreatedAt = DateTime.UtcNow;
        }
        public long Id { get; set; }
        public long UserId { get; set; }
        public long ProductId { get; set; }
        public DateTime CreatedAt { get; set; }

        public required User User { get; set; }
        public required Product Product { get; set; }
    }
}
