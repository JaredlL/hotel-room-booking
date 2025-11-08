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
        var hotelEntity = modelBuilder.Entity<Hotel>();

        // For simplicity the hotel name is used as the primary key.
        // However, if there was a requirement to rename hotels or support duplicate names,
        // a DB-managed long key would support greater flexibility.
        hotelEntity.HasKey(x => x.Name);

        hotelEntity.HasMany(x => x.Rooms)
            .WithOne(x => x.Hotel)
            .OnDelete(DeleteBehavior.Cascade);

        hotelEntity.Ignore(x => x.Links);

        var roomEntity = modelBuilder.Entity<Room>();
        roomEntity.HasKey(x => x.Id);
        roomEntity.HasOne(x => x.Hotel);
        roomEntity.Property(x => x.RoomType)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<RoomType>(v));

        roomEntity.HasMany(x => x.BookedNights);

        var bookingEntity = modelBuilder.Entity<Booking>();
        bookingEntity.HasKey(x => x.BookingReference);
        bookingEntity.HasOne(x => x.BookedRoom);

        bookingEntity.HasMany(x => x.BookedNights)
            .WithOne()
            .HasForeignKey(x => x.BookingId)
            .IsRequired();

        var bookedNightEntity = modelBuilder.Entity<BookedNight>();

        // This composite key ensures a room cannot be double booked for a night.
        // It should also enable efficient queries for finding all bookings for a room on a given day.
        bookedNightEntity.HasKey(x => new { x.RoomId, x.Date });

        bookedNightEntity.HasOne<Room>()
            .WithMany(r => r.BookedNights)
            .HasForeignKey(x => x.RoomId)
            .IsRequired();

        base.OnModelCreating(modelBuilder);
    }
}