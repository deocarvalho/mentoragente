-- Migration: Add WhatsApp Provider Support
-- Description: Adds whatsapp_provider enum and instance_code column, migrates existing data
-- Date: 2025-01-XX

-- Step 1: Create WhatsApp provider enum type
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'whatsapp_provider') THEN
        CREATE TYPE whatsapp_provider AS ENUM ('EvolutionAPI', 'ZApi', 'OfficialWhatsApp');
    END IF;
END $$;

-- Step 2: Add new columns to mentorships table
ALTER TABLE mentorships 
    ADD COLUMN IF NOT EXISTS whatsapp_provider whatsapp_provider DEFAULT 'ZApi',
    ADD COLUMN IF NOT EXISTS instance_code VARCHAR;

-- Step 3: Migrate existing data from evolution_instance_name to instance_code
UPDATE mentorships 
SET instance_code = evolution_instance_name 
WHERE instance_code IS NULL AND evolution_instance_name IS NOT NULL AND evolution_instance_name != '';

-- Step 4: Set default provider for existing records (if they have evolution_instance_name, assume EvolutionAPI)
UPDATE mentorships 
SET whatsapp_provider = 'EvolutionAPI'::whatsapp_provider
WHERE whatsapp_provider IS NULL 
  AND evolution_instance_name IS NOT NULL 
  AND evolution_instance_name != '';

-- Step 5: Add constraint to ensure instance_code is set when mentorship is active
-- (Optional - can be enforced at application level instead)
-- ALTER TABLE mentorships 
--     ADD CONSTRAINT check_instance_code_when_active 
--     CHECK (status != 'Active' OR instance_code IS NOT NULL);

-- Step 6: Create index on whatsapp_provider for faster queries
CREATE INDEX IF NOT EXISTS idx_mentorships_whatsapp_provider ON mentorships(whatsapp_provider);

-- Step 7: Create index on instance_code for faster lookups
CREATE INDEX IF NOT EXISTS idx_mentorships_instance_code ON mentorships(instance_code);

-- Note: evolution_api_key and evolution_instance_name columns are kept for backward compatibility
-- They can be removed in a future migration after confirming all mentorships are migrated

