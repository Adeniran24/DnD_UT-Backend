using GameApi.Models;
using Microsoft.EntityFrameworkCore;
using GameApi.Controllers;
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
        public DbSet<CommunityInvite> CommunityInvites { get; set; }
        public DbSet<CommunityMessageReaction> CommunityMessageReactions { get; set; }
        public DbSet<VoiceChannelState> VoiceChannelStates { get; set; }

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
        public DbSet<Character> Characters { get; set; }
        public DbSet<VttSession> VttSessions { get; set; }
        public DbSet<VttSessionMember> VttSessionMembers { get; set; }
        public DbSet<VttMap> VttMaps { get; set; }
        public DbSet<VttToken> VttTokens { get; set; }
        public DbSet<VttChatMessage> VttChatMessages { get; set; }
        public DbSet<VttAsset> VttAssets { get; set; }
        public DbSet<VttInitiativeEntry> VttInitiativeEntries { get; set; }

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

            modelBuilder.Entity<Channel>()
                .HasMany(ch => ch.Children)
                .WithOne(ch => ch.Parent)
                .HasForeignKey(ch => ch.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

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

            modelBuilder.Entity<CommunityInvite>()
                .HasIndex(ci => ci.Code)
                .IsUnique();

            modelBuilder.Entity<CommunityInvite>()
                .HasOne(ci => ci.Community)
                .WithMany(c => c.Invites)
                .HasForeignKey(ci => ci.CommunityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommunityInvite>()
                .HasOne(ci => ci.CreatedBy)
                .WithMany(u => u.CommunityInvitesCreated)
                .HasForeignKey(ci => ci.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommunityMessageReaction>()
                .HasIndex(r => new { r.MessageId, r.UserId, r.Emoji })
                .IsUnique();

            modelBuilder.Entity<CommunityMessageReaction>()
                .HasOne(r => r.Message)
                .WithMany(m => m.Reactions)
                .HasForeignKey(r => r.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommunityMessageReaction>()
                .HasOne(r => r.User)
                .WithMany(u => u.CommunityMessageReactions)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VoiceChannelState>()
                .HasKey(vs => new { vs.ChannelId, vs.UserId });

            modelBuilder.Entity<VoiceChannelState>()
                .HasOne(vs => vs.Channel)
                .WithMany(ch => ch.VoiceStates)
                .HasForeignKey(vs => vs.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VoiceChannelState>()
                .HasOne(vs => vs.User)
                .WithMany(u => u.VoiceChannelStates)
                .HasForeignKey(vs => vs.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // -------------------------------
            // MapForge share composite key
            // -------------------------------
            modelBuilder.Entity<MapForgeController.MapCampaignShare>()
                .HasKey(ms => new { ms.CampaignId, ms.UserId });

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
            // -------------------------------
            modelBuilder.Entity<Character>()
    .Property(c => c.created_at)
    .HasDefaultValueSql("CURRENT_TIMESTAMP");

modelBuilder.Entity<Character>()
    .Property(c => c.updated_at)
    .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

            
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


            modelBuilder.Entity<VttSession>()
                .HasOne(v => v.Owner)
                .WithMany()
                .HasForeignKey(v => v.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VttSessionMember>()
                .HasKey(m => new { m.SessionId, m.UserId });

            modelBuilder.Entity<VttSessionMember>()
                .HasOne(m => m.Session)
                .WithMany(s => s.Members)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VttSessionMember>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VttMap>()
                .HasOne(m => m.Session)
                .WithMany(s => s.Maps)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VttToken>()
                .HasOne(t => t.Session)
                .WithMany(s => s.Tokens)
                .HasForeignKey(t => t.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VttToken>()
                .HasOne(t => t.Owner)
                .WithMany()
                .HasForeignKey(t => t.OwnerUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<VttToken>()
                .HasOne(t => t.Character)
                .WithMany()
                .HasForeignKey(t => t.CharacterId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<VttChatMessage>()
                .HasOne(m => m.Session)
                .WithMany(s => s.ChatMessages)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VttChatMessage>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VttInitiativeEntry>()
                .HasOne(i => i.Session)
                .WithMany(s => s.InitiativeEntries)
                .HasForeignKey(i => i.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VttInitiativeEntry>()
                .HasOne(i => i.Token)
                .WithMany()
                .HasForeignKey(i => i.TokenId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<VttAsset>()
                .HasOne(a => a.Session)
                .WithMany(s => s.Assets)
                .HasForeignKey(a => a.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VttAsset>()
                .HasOne(a => a.UploadedBy)
                .WithMany()
                .HasForeignKey(a => a.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            
        }
        public DbSet<MapForgeController.MapCampaign> MapCampaigns { get; set; }
        public DbSet<MapForgeController.MapCampaignShare> MapCampaignShares { get; set; }

        
    }
    
}
