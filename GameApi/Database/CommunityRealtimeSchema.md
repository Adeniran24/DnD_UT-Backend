Community Realtime Schema (Discord-style)

Core tables:
- Communities (servers)
  - Id (PK), Name, Description, OwnerId (FK Users), IsPrivate, CoverImage, CreatedAt, UpdatedAt
- CommunityUsers (membership)
  - CommunityId (FK Communities), UserId (FK Users), Role, Nickname
  - PK (CommunityId, UserId)
- Channels
  - Id (PK), CommunityId (FK Communities), Name, Type (Text/Voice/Category/News)
  - IsPrivate, Topic, Position, ParentId (self FK), IsArchived, IsReadOnly
- CommunityMessages
  - Id (PK), ChannelId (FK Channels), SenderId (FK Users), Content, Timestamp
  - EditedAt, IsPinned, IsDeleted, RecipientId (optional FK Users)
- CommunityMessageReactions
  - Id (PK), MessageId (FK CommunityMessages), UserId (FK Users), Emoji, CreatedAt
  - Unique index (MessageId, UserId, Emoji)
- CommunityInvites
  - Id (PK), CommunityId (FK Communities), Code (unique), CreatedById (FK Users)
  - Uses, MaxUses, ExpiresAt, CreatedAt
- VoiceChannelStates
  - ChannelId (FK Channels), UserId (FK Users), IsMuted, IsDeafened, IsStreaming, JoinedAt
  - PK (ChannelId, UserId)

Relationships:
- Communities 1..* Channels
- Communities 1..* CommunityUsers
- Channels 1..* CommunityMessages
- CommunityMessages 1..* CommunityMessageReactions
- Channels (self) ParentId for categories

Notes:
- News channels are read-only for members (enforced in controllers/hub).
- Voice uses SignalR for signaling; add SFU later for real media routing.
