using AscentService.Domain.Ascents;
using Common.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AscentService.Infrastructure.Persistence.Configurations;

internal sealed class AscentConfiguration : EntityConfiguration<Ascent>
{
    private const int VisibilityLength = 20;

    public override void Configure(EntityTypeBuilder<Ascent> builder)
    {
        base.Configure(builder);

        builder.ToTable("ascents");

        ConfigureColumns(builder);
        ConfigureIndexes(builder);
        ConfigurePeakSnapshot(builder);
        ConfigureConditions(builder);
        ConfigurePhotos(builder);
    }

    private static void ConfigureColumns(EntityTypeBuilder<Ascent> builder)
    {
        builder.Property(ascent => ascent.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(ascent => ascent.AscentDate).HasColumnName("ascent_date").IsRequired();

        builder.Property(ascent => ascent.Companions)
            .HasColumnName("companions").HasMaxLength(Ascent.MaxCompanionsLength);

        builder.Property(ascent => ascent.RouteNotes).HasColumnName("route_notes");

        builder.Property(ascent => ascent.Visibility)
            .HasColumnName("visibility")
            .HasMaxLength(VisibilityLength)
            .HasConversion<string>()
            .HasDefaultValue(AscentVisibility.Public)
            .IsRequired();
    }

    private static void ConfigureIndexes(EntityTypeBuilder<Ascent> builder)
    {
        builder.HasIndex(ascent => new { ascent.UserId, ascent.AscentDate })
            .IsDescending(false, true)
            .HasDatabaseName("ix_ascents_user_date");
    }

    private static void ConfigurePeakSnapshot(EntityTypeBuilder<Ascent> builder)
    {
        builder.OwnsOne(ascent => ascent.Peak, peak =>
        {
            peak.Property(snapshot => snapshot.PeakId).HasColumnName("peak_id").IsRequired();
            peak.Property(snapshot => snapshot.Name)
                .HasColumnName("peak_name").HasMaxLength(PeakSnapshot.MaxNameLength).IsRequired();
            peak.Property(snapshot => snapshot.AltitudeMeters).HasColumnName("peak_altitude_m").IsRequired();

            peak.HasIndex(snapshot => snapshot.PeakId).HasDatabaseName("ix_ascents_peak");
        });

        builder.Navigation(ascent => ascent.Peak).IsRequired();
    }

    private static void ConfigureConditions(EntityTypeBuilder<Ascent> builder)
    {
        builder.OwnsOne(ascent => ascent.Conditions, conditions => conditions.ToJson("conditions"));

        builder.Navigation(ascent => ascent.Conditions).IsRequired();
    }

    private static void ConfigurePhotos(EntityTypeBuilder<Ascent> builder)
    {
        builder.OwnsMany(ascent => ascent.Photos, photo =>
        {
            photo.ToTable("ascent_photos");
            photo.WithOwner().HasForeignKey("ascent_id");
            photo.HasKey(entity => entity.Id);

            photo.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
            photo.Property<Guid>("ascent_id").HasColumnName("ascent_id");
            photo.Property(entity => entity.CloudinaryPublicId)
                .HasColumnName("cloudinary_public_id").HasMaxLength(PhotoUpload.MaxPublicIdLength).IsRequired();
            photo.Property(entity => entity.SecureUrl)
                .HasColumnName("secure_url").HasMaxLength(PhotoUpload.MaxSecureUrlLength).IsRequired();
            photo.Property(entity => entity.Width).HasColumnName("width").IsRequired();
            photo.Property(entity => entity.Height).HasColumnName("height").IsRequired();
            photo.Property(entity => entity.Position).HasColumnName("position").IsRequired();
            photo.Property(entity => entity.UploadedAtUtc).HasColumnName("uploaded_at_utc").IsRequired();

            photo.HasIndex("ascent_id", nameof(AscentPhoto.Position))
                .IsUnique().HasDatabaseName("ux_ascent_photo_position");
        });

        builder.Navigation(ascent => ascent.Photos).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
