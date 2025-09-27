-- Initialize YouTube Downloader Database
-- This script sets up the initial database configuration

-- Create extensions if they don't exist
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Set timezone
SET timezone = 'UTC';

-- Create indexes for better performance (these will be created by EF migrations, but good to have as backup)
-- Note: The actual table creation is handled by Entity Framework migrations