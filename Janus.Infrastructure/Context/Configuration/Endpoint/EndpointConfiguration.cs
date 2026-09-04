using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Janus.Infrastructure.Context.Configuration.Endpoint;

public class EndpointConfiguration : IEntityTypeConfiguration<Domain.Models.EndpointDomain>
{
    public void Configure(EntityTypeBuilder<Domain.Models.EndpointDomain> builder)
    {
        builder.ToTable("Endpoints");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ClientName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ClientRoute)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Method)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(x => x.Enabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasIndex(x => new
            {
                x.Method,
                x.ClientRoute
            })
            .IsUnique();

        builder.HasIndex(x => x.Enabled);
    }
}