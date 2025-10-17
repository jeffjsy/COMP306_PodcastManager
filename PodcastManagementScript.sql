

BEGIN;
CREATE TABLE [__EFMigrationsHistory] (
	[MigrationId] nvarchar(150) NOT NULL,
	[ProductVersion] nvarchar(32) NOT NULL,
	CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
);

CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(128) NOT NULL,
    [ProviderKey] nvarchar(128) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);




CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(128) NOT NULL,
    [Name] nvarchar(128) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'00000000000000_CreateIdentitySchema', N'8.0.8');

BEGIN TRANSACTION;

CREATE TABLE [Podcasts] (
    [PodcastID] int NOT NULL IDENTITY,
    [Title] nvarchar(255) NOT NULL,           
    [Description] nvarchar(max) NOT NULL,
    [CreatorID] nvarchar(450) NOT NULL,        
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Podcasts] PRIMARY KEY ([PodcastID])
);

CREATE TABLE [Episodes] (
    [EpisodeID] int NOT NULL IDENTITY,
    [PodcastID] int NOT NULL,
    [Title] nvarchar(255) NOT NULL,            
    [ReleaseDate] datetime2 NOT NULL,
    [DurationMinutes] int NOT NULL,
    [PlayCount] int NOT NULL,
    [AudioFileURL] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Episodes] PRIMARY KEY ([EpisodeID]),
    CONSTRAINT [FK_Episodes_Podcasts_PodcastID] FOREIGN KEY ([PodcastID]) REFERENCES [Podcasts] ([PodcastID]) ON DELETE CASCADE
);

CREATE TABLE [Subscriptions] (
    [SubscriptionID] int NOT NULL IDENTITY,
    [UserID] nvarchar(450) NOT NULL,          
    [PodcastID] int NOT NULL,
    [SubscribedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([SubscriptionID]),
    CONSTRAINT [FK_Subscriptions_Podcasts_PodcastID] FOREIGN KEY ([PodcastID]) REFERENCES [Podcasts] ([PodcastID]) ON DELETE CASCADE
);

COMMIT;

-- --------------------------------------------------------------------------------------------------
-- DML SECTION: SAMPLE DATA INSERTS
-- --------------------------------------------------------------------------------------------------
BEGIN TRANSACTION;


INSERT INTO Podcasts (Title, Description, CreatorID, CreatedDate) VALUES
('Moonshot Leadership', 'Exploring exponential technologies and future trends.', 'user-guid-peter', '2024-01-15 08:00:00'),
('The Future of AI', 'Weekly discussions on machine learning and ethics.', 'user-guid-admin', '2024-02-20 10:30:00'),
('Space Exploration News', 'Updates on NASA, SpaceX, and commercial space ventures.', 'user-guid-podcaster1', '2024-03-01 12:00:00'),
('BioHacking Basics', 'Tips and tricks for optimizing human performance.', 'user-guid-podcaster2', '2024-03-10 09:15:00'),
('Cloud Native Deep Dive', 'Focusing on AWS, Kubernetes, and API engineering.', 'user-guid-admin', '2024-04-05 14:45:00');


INSERT INTO Episodes (PodcastID, Title, ReleaseDate, DurationMinutes, PlayCount, AudioFileURL) VALUES
(1, 'Episode 1: AI & Longevity', '2024-01-20 15:00:00', 45, 12500, 'https://s3.amazonaws.com/comp306-podcast-media-bucket/p1e1.mp3'),
(2, 'Ethical AI: Bias in Algorithms', '2024-03-01 11:00:00', 62, 5800, 'https://s3.amazonaws.com/comp306-podcast-media-bucket/p2e1.mp3'),
(1, 'Episode 2: Hyperloop Transportation', '2024-02-15 09:00:00', 38, 15100, 'https://s3.amazonaws.com/comp306-podcast-media-bucket/p1e2.mp3'),
(3, 'Starship Launch Update Q2 2024', '2024-04-10 13:30:00', 25, 3200, 'https://s3.amazonaws.com/comp306-podcast-media-bucket/p3e1.mp3'),
(5, 'Deploying ASP.NET Core on Elastic Beanstalk', '2024-04-15 16:00:00', 50, 950, 'https://s3.amazonaws.com/comp306-podcast-media-bucket/p5e1.mp3');


INSERT INTO Subscriptions (UserID, PodcastID, SubscribedDate) VALUES
('listener-guid-1', 1, '2024-01-20 00:00:00'),
('listener-guid-2', 1, '2024-01-25 00:00:00'),
('listener-guid-3', 2, '2024-03-05 00:00:00'),
('listener-guid-1', 3, '2024-04-12 00:00:00'),
('listener-guid-4', 5, '2024-04-16 00:00:00');


COMMIT;
