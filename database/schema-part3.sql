-- Part 3: image URL columns (Azure Blob Storage). Run this on an existing RaceDayDB created in Part 2.
-- (Alternative: drop RaceDayDB and restart the API - it recreates the schema from the EF Core model.)
USE RaceDayDB;
GO

IF COL_LENGTH('Events', 'BannerImageUrl') IS NULL
    ALTER TABLE Events ADD BannerImageUrl NVARCHAR(500) NULL;
GO

IF COL_LENGTH('Users', 'ProfileImageUrl') IS NULL
    ALTER TABLE Users ADD ProfileImageUrl NVARCHAR(500) NULL;
GO
