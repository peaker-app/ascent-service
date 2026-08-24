using AscentService.Domain.ConfirmedUsers;
using Common.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AscentService.Infrastructure.Persistence.Configurations;

internal sealed class ConfirmedUserConfiguration : EntityConfiguration<ConfirmedUser>
{
    public override void Configure(EntityTypeBuilder<ConfirmedUser> builder)
    {
        base.Configure(builder);

        builder.ToTable("confirmed_users");

        builder.Property(confirmedUser => confirmedUser.ConfirmedAtUtc)
            .HasColumnName("confirmed_at_utc")
            .IsRequired();
    }
}
