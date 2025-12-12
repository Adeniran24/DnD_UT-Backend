using GameApi.Models;
using Microsoft.EntityFrameworkCore;

// Alias az ütköző név elkerülésére:
using DNDProficiency = GameApi.Models.DND2014.Proficiency;
using DNDClass = GameApi.Models.DND2014.Class;
using DNDSubclass = GameApi.Models.DND2014.Subclass;
using DNDStartingEquipment = GameApi.Models.DND2014.StartingEquipment;
using DNDProficiencyChoice = GameApi.Models.DND2014.ProficiencyChoice;
using DNDMultiClassing = GameApi.Models.DND2014.MultiClassing;
using DNDPrerequisite = GameApi.Models.DND2014.Prerequisite;

namespace GameApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // -------------------------------
        // Existing App Entities
        // -------------------------------
        public DbSet<User> Users { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<PdfFile> PdfFiles { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<ChatRoomUser> ChatRoomUsers { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<PdfFormData> PdfFormDatas { get; set; }

        // Community system
        public DbSet<Community> Communities { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<CommunityMessage> CommunityMessages { get; set; }
        public DbSet<CommunityUser> CommunityUsers { get; set; }

        // -------------------------------
        // DND2014 Entities
        // -------------------------------
        public DbSet<DNDClass> DNDClasses { get; set; }
        public DbSet<DNDProficiency> DNDProficiencies { get; set; }
        public DbSet<DNDSubclass> DNDSubclasses { get; set; }
        public DbSet<DNDStartingEquipment> DNDStartingEquipment { get; set; }
        public DbSet<DNDProficiencyChoice> DNDProficiencyChoices { get; set; }
        public DbSet<DNDMultiClassing> DNDMultiClassings { get; set; }
        public DbSet<DNDPrerequisite> DNDPrerequisites { get; set; }
        public DbSet<DirectMessage> DirectMessages { get; set; }

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
            // ChatRoom -> Messages
            // -------------------------------
            modelBuilder.Entity<ChatRoom>()
                .HasMany(cr => cr.Messages)
                .WithOne(m => m.ChatRoom)
                .HasForeignKey(m => m.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // -------------------------------
            // Message relationships
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
            // Community -> Channels -> Messages
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

            // -------------------------------
            // DND2014 Relationships
            // -------------------------------

            // Class -> Proficiencies
            modelBuilder.Entity<DNDProficiency>()
                .HasOne(p => p.Class)
                .WithMany(c => c.Proficiencies)
                .HasForeignKey(p => p.ClassId);

            // Class -> Subclasses
            modelBuilder.Entity<DNDSubclass>()
                .HasOne(s => s.Class)
                .WithMany(c => c.Subclasses)
                .HasForeignKey(s => s.ClassId);

            // Class -> StartingEquipment
            modelBuilder.Entity<DNDStartingEquipment>()
                .HasOne(e => e.Class)
                .WithMany(c => c.StartingEquipment)
                .HasForeignKey(e => e.ClassId);

            // Class -> ProficiencyChoices
            modelBuilder.Entity<DNDProficiencyChoice>()
                .HasOne(pc => pc.Class)
                .WithMany(c => c.ProficiencyChoices)
                .HasForeignKey(pc => pc.ClassId);

            // Class -> MultiClassing (1:1)
            modelBuilder.Entity<DNDMultiClassing>()
                .HasOne(m => m.Class)
                .WithOne(c => c.MultiClassing)
                .HasForeignKey<DNDMultiClassing>(m => m.ClassId);

            // MultiClassing -> Prerequisites
            modelBuilder.Entity<DNDPrerequisite>()
                .HasOne(p => p.MultiClassing)
                .WithMany(m => m.Prerequisites)
                .HasForeignKey(p => p.MultiClassingId);

            
            modelBuilder.Entity<DirectMessage>()
    .HasOne(dm => dm.Sender)
    .WithMany()
    .HasForeignKey(dm => dm.SenderId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<DirectMessage>()
    .HasOne(dm => dm.Receiver)
    .WithMany()
    .HasForeignKey(dm => dm.ReceiverId)
    .OnDelete(DeleteBehavior.Restrict);


            
        }
        
    }
}
