using Flickoo.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flickoo.Api.Data.Configurations
{
    public class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
    {
        public void Configure(EntityTypeBuilder<MediaFile> builder)
        {
            builder.ToTable("MediaFiles").HasKey(mf => mf.Id);
            builder.Property(mf => mf.Id).HasColumnName("MediaId");
            builder.Property(mf => mf.Url).IsRequired();
            builder.Property(mf => mf.TypeOfMedia).IsRequired();
            builder.HasOne(mf => mf.Product)
                .WithMany(p => p.ProductMedias)
                .HasForeignKey(mf => mf.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
