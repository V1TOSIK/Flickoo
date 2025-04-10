using Flickoo.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flickoo.Api.Data.Configurations
{
    public class MediaConfiguration : IEntityTypeConfiguration<Media>
    {
        public void Configure(EntityTypeBuilder<Media> builder)
        {
            builder.ToTable("Medias")
                .HasKey(mf => mf.Id);
            
            builder.Property(mf => mf.Id)
                .HasColumnName("MediaId");
            
            builder.Property(mf => mf.Url)
                .IsRequired();
            
            builder.Property(mf => mf.TypeOfMedia)
                .HasConversion<string>()
                .IsRequired();
            
            builder.HasOne(mf => mf.Product)
                .WithMany(p => p.ProductMedias)
                .HasForeignKey(mf => mf.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
