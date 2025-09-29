using GameApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Alap entitások
        public DbSet<User> Users { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<PdfFile> PdfFiles { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<ChatRoomUser> ChatRoomUsers { get; set; }
        public DbSet<Message> Messages { get; set; } // ChatRoom üzenetek
        public DbSet<Book> Books { get; set; }

        // Community rendszer
        public DbSet<Community> Communities { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<CommunityMessage> CommunityMessages { get; set; }

        public DbSet<CommunityUser> CommunityUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -------------------------------
            // ChatRoomUser composite key
            // -------------------------------
            modelBuilder.Entity<ChatRoomUser>()
                .HasKey(cu => new { cu.ChatRoomId, cu.UserId });

            modelBuilder.Entity<ChatRoomUser>()
                .HasOne(cu => cu.ChatRoom)
                .WithMany(cr => cr.Users)
                .HasForeignKey(cu => cu.ChatRoomId);

            modelBuilder.Entity<ChatRoomUser>()
                .HasOne(cu => cu.User)
                .WithMany(u => u.ChatRooms)
                .HasForeignKey(cu => cu.UserId);

            // -------------------------------
            // Friendship composite key
            // -------------------------------
            modelBuilder.Entity<Friendship>()
                .HasKey(f => new { f.RequesterId, f.AddresseeId });

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Requester)
                .WithMany(u => u.SentFriendRequests)
                .HasForeignKey(f => f.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Addressee)
                .WithMany(u => u.ReceivedFriendRequests)
                .HasForeignKey(f => f.AddresseeId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------------------
            // ChatRoom -> Messages kapcsolat
            // -------------------------------
            modelBuilder.Entity<ChatRoom>()
                .HasMany(cr => cr.Messages)
                .WithOne(m => m.ChatRoom)
                .HasForeignKey(m => m.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // -------------------------------
            // Message kapcsolatok
            // -------------------------------
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Recipient)
                .WithMany()
                .HasForeignKey(m => m.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------------------
            // Community -> Channel -> CommunityMessages
            // -------------------------------
            modelBuilder.Entity<Community>()
                .HasMany(c => c.Channels)
                .WithOne(ch => ch.Community)
                .HasForeignKey(ch => ch.CommunityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Channel>()
                .HasMany(ch => ch.Messages)
                .WithOne(m => m.Channel)
                .HasForeignKey(m => m.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommunityMessage>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.CommunityMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommunityMessage>()
                .HasOne(m => m.Recipient)
                .WithMany()
                .HasForeignKey(m => m.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------------------
            // CommunityUser composite key
            // -------------------------------
            modelBuilder.Entity<CommunityUser>()
                .HasKey(cu => new { cu.CommunityId, cu.UserId });
        }
    }
}
