using Flickoo.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flickoo.Api.Data.Configurations
{
    public class LikeConfiguration : IEntityTypeConfiguration<Like>
    {
        public void Configure(EntityTypeBuilder<Like> builder)
        {
            builder.ToTable("Likes").HasKey(l => l.Id);
            builder.Property(l => l.Id).HasColumnName("LikeId");
            builder.HasOne(l => l.Product)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(l => l.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}