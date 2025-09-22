using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Demo.Models;

#nullable disable warnings

public class DB : DbContext
{
    public DB(DbContextOptions<DB> options) : base(options) { }

    // DbSet
    public DbSet<User> Users { get; set; }
    public DbSet<RoomType> RoomTypes { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<RoomImage> RoomImages { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Relationships
        modelBuilder.Entity<User>()
            .HasMany(u => u.Reservations)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.Email)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Room>()
            .HasMany(r => r.Reservations)
            .WithOne(res => res.Room)
            .HasForeignKey(res => res.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Room>()
            .HasOne(r => r.RoomType)
            .WithMany(rt => rt.Rooms)
            .HasForeignKey(r => r.TypeId);
    }
}

// ---------- Supplementary types ----------
public enum ReservationStatus
{
    Pending,
    CheckedIn,
    CheckedOut,
    Cancelled
}

// Entity Classes -------------------------------------------------------------

public class User
{
    [Key, MaxLength(100)]
    public string Email { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }

    [Required, MaxLength(64)]
    public string Password { get; set; }

    [MaxLength(100)]
    public string PhotoURL { get; set; }

    [MaxLength(10)]
    public string Role { get; set; }

    // lockout fields
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; }

    // Navigation
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}

public class RoomType
{
    [Key, MaxLength(5)]
    public string TypeId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }

    [Required, Precision(6, 2)]
    public decimal Price { get; set; }

    // Navigation Properties
    public List<Room> Rooms { get; set; } = new List<Room>();
}

public class Room
{
    [Key, MaxLength(5)]
    public string RoomId { get; set; }

    // FK
    public string TypeId { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    // Navigation
    [ForeignKey("TypeId")]
    public RoomType RoomType { get; set; }
    public List<Reservation> Reservations { get; set; } = new List<Reservation>();

    public List<RoomImage> RoomImages { get; set; } = new List<RoomImage>();
}

public class RoomImage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string RoomId { get; set; }

    [Required, MaxLength(200)]
    public string ImageUrl { get; set; }

    [ForeignKey("RoomId")]
    public Room Room { get; set; }
}

public class Reservation
{
    [Key, MaxLength(50)]
    public string ReservationId { get; set; } = Guid.NewGuid().ToString(); // or your own scheme

    public DateOnly CheckIn { get; set; }

    public DateOnly CheckOut { get; set; }

    [Precision(6, 2)]
    public decimal TotalPrice { get; set; }

    // FK
    public string Email { get; set; }
    public string RoomId { get; set; }

    // Navigation Properties
    public User User { get; set; }
    public Room Room { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    // Helper to know if currently occupied/active
    public bool IsActive(DateOnly current)
    {
        if (Status == ReservationStatus.CheckedIn) return true;
        if (Status == ReservationStatus.Pending)
        {
            return current >= CheckIn && current < CheckOut;
        }
        return false;
    }
}
