using AscentService.Domain.DeletedUsers;
using Common.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AscentService.Infrastructure.Persistence.Configurations;

internal sealed class DeletedUserConfiguration : EntityConfiguration<DeletedUser>
{
    public override void Configure(EntityTypeBuilder<DeletedUser> builder)
    {
        base.Configure(builder);

        builder.ToTable("deleted_users");

        builder.Property(deletedUser => deletedUser.DeletedAtUtc)
            .HasColumnName("deleted_at_utc")
            .IsRequired();
    }
}
