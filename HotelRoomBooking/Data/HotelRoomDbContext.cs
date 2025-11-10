using HotelRoomBooking.Domain;
using Microsoft.EntityFrameworkCore;

namespace HotelRoomBooking.Data;

public class HotelRoomDbContext(DbContextOptions<HotelRoomDbContext> contextOptions) 
    : DbContext(contextOptions)
{
    public DbSet<Hotel> Hotels { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookedNight> BookedNights { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hotel>(h =>
        {
            // For simplicity the hotel name is used as the primary key.
            // However, if there was a requirement to rename hotels or support duplicate names,
            // a DB-managed long key would support greater flexibility.
            h.HasKey(x => x.Name);

            h.HasMany(x => x.Rooms)
                .WithOne(x => x.Hotel)
                .OnDelete(DeleteBehavior.Cascade);

            h.Ignore(x => x.Links);
        });

        modelBuilder.Entity<Room>(r =>
        {
            r.HasKey(x => x.Id);
            r.HasOne(x => x.Hotel)
                .WithMany(x => x.Rooms)
                .IsRequired();

            r.Property(x => x.RoomType)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<RoomType>(v));

            r.HasMany(x => x.BookedNights);
        });

        modelBuilder.Entity<Booking>(b =>
        {
            b.HasKey(x => x.BookingReference);

            // Ensure that the booking reference is auto-incremented and
            // that app code cannot specify a booking reference.
            // This removes the risk that a reference is accidentally reused.
            b.Property(x => x.BookingReference)
                .UseIdentityAlwaysColumn();

            b.HasOne(x => x.BookedRoom);

            b.HasMany(x => x.BookedNights)
                .WithOne()
                .HasForeignKey(x => x.BookingId)
                .IsRequired();
        });

        modelBuilder.Entity<BookedNight>(bn =>
        {
            // This composite key ensures a room cannot be double booked for a night.
            // It should also enable efficient queries for finding all bookings for a room on a given day.
            bn.HasKey(x => new { x.RoomId, x.Date });

            bn.HasOne<Room>()
                .WithMany(r => r.BookedNights)
                .HasForeignKey(x => x.RoomId)
                .IsRequired();
        });
    }
}