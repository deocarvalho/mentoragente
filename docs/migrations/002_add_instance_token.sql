-- Migration: Add Instance Token Support
-- Description: Adds instance_token column for Z-API instance-specific tokens
-- Date: 2025-01-XX

-- Step 1: Add instance_token column to mentorships table
ALTER TABLE mentorships 
    ADD COLUMN IF NOT EXISTS instance_token VARCHAR;

-- Step 2: Add comment to document the column purpose
COMMENT ON COLUMN mentorships.instance_token IS 'Provider-specific instance token (e.g., Z-API instance token). Required for Z-API, optional for other providers.';

-- Step 3: Create index on instance_token for faster lookups (optional, but useful if querying by token)
CREATE INDEX IF NOT EXISTS idx_mentorships_instance_token ON mentorships(instance_token) WHERE instance_token IS NOT NULL;

