-- ============================================
-- MENTORAGENTE DATABASE SCHEMA
-- Database: Mentoragente (Supabase PostgreSQL)
-- ============================================

-- ============================================
-- ENUMS (PostgreSQL ENUM types)
-- ============================================

-- User Status
CREATE TYPE user_status AS ENUM ('Active', 'Inactive', 'Blocked');

-- Agent Session Status
CREATE TYPE agent_session_status AS ENUM ('Active', 'Expired', 'Paused', 'Completed');

-- AI Provider
CREATE TYPE ai_provider AS ENUM ('OpenAI');

-- Mentorship Status
CREATE TYPE mentorship_status AS ENUM ('Active', 'Inactive', 'Archived');

-- ============================================
-- TABLES
-- ============================================

-- Users (pessoas físicas identificadas por telefone)
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    phone_number VARCHAR UNIQUE NOT NULL,
    name VARCHAR NOT NULL,
    email VARCHAR NULL,
    status user_status NOT NULL DEFAULT 'Active',
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Mentorships (mentorship programs created by mentors)
CREATE TABLE mentorships (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR NOT NULL,
    mentor_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    assistant_id VARCHAR NOT NULL,  -- OpenAI Assistant ID
    duration_days INT NOT NULL,  -- 30, 60, 90, etc.
    description TEXT NULL,
    status mentorship_status NOT NULL DEFAULT 'Active',
    evolution_api_key VARCHAR NOT NULL DEFAULT '',
    evolution_instance_name VARCHAR NOT NULL DEFAULT '',
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Agent Sessions (agent sessions - links User + Mentorship)
CREATE TABLE agent_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    mentorship_id UUID NOT NULL REFERENCES mentorships(id) ON DELETE CASCADE,
    ai_provider ai_provider NOT NULL DEFAULT 'OpenAI',
    ai_context_id VARCHAR NULL,  -- OpenAI Thread ID
    status agent_session_status NOT NULL DEFAULT 'Active',
    last_interaction TIMESTAMP NULL,
    total_messages INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Agent Session Data (dados da sessão - propriedades comuns)
CREATE TABLE agent_session_data (
    agent_session_id UUID PRIMARY KEY REFERENCES agent_sessions(id) ON DELETE CASCADE,
    access_start_date TIMESTAMP NOT NULL,
    access_end_date TIMESTAMP NOT NULL,
    progress_percentage INT NOT NULL DEFAULT 0,
    report_generated BOOLEAN NOT NULL DEFAULT FALSE,
    report_generated_at TIMESTAMP NULL,
    admin_notes TEXT NULL,
    custom_properties_json JSONB NULL,  -- Para customizações futuras (vazio por enquanto)
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Conversations (histórico de mensagens)
CREATE TABLE conversations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    agent_session_id UUID NOT NULL REFERENCES agent_sessions(id) ON DELETE CASCADE,
    sender VARCHAR NOT NULL,  -- 'user' ou 'assistant'
    message TEXT NOT NULL,
    message_type VARCHAR NOT NULL DEFAULT 'text',
    tokens_used INT NULL,
    response_time_ms INT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- ============================================
-- INDEXES
-- ============================================

-- Users indexes
CREATE INDEX idx_users_phone_number ON users(phone_number);
CREATE INDEX idx_users_email ON users(email) WHERE email IS NOT NULL;
CREATE INDEX idx_users_status ON users(status);

-- Mentorships indexes
CREATE INDEX idx_mentorships_mentor_id ON mentorships(mentor_id);
CREATE INDEX idx_mentorships_status ON mentorships(status);
CREATE INDEX idx_mentorships_assistant_id ON mentorships(assistant_id);

-- Agent Sessions indexes
CREATE INDEX idx_agent_sessions_user_id ON agent_sessions(user_id);
CREATE INDEX idx_agent_sessions_mentorship_id ON agent_sessions(mentorship_id);
CREATE INDEX idx_agent_sessions_status ON agent_sessions(status);
CREATE INDEX idx_agent_sessions_ai_context_id ON agent_sessions(ai_context_id) WHERE ai_context_id IS NOT NULL;
CREATE INDEX idx_agent_sessions_last_interaction ON agent_sessions(last_interaction);

-- Unique index: a user can have only one active session per mentorship
CREATE UNIQUE INDEX idx_agent_sessions_unique_active 
    ON agent_sessions(user_id, mentorship_id) 
    WHERE status = 'Active';

-- Agent Session Data indexes
CREATE INDEX idx_agent_session_data_access_end ON agent_session_data(access_end_date);
CREATE INDEX idx_agent_session_data_progress ON agent_session_data(progress_percentage);
CREATE INDEX idx_agent_session_data_custom_props ON agent_session_data USING GIN (custom_properties_json) WHERE custom_properties_json IS NOT NULL;

-- Conversations indexes
CREATE INDEX idx_conversations_agent_session_id ON conversations(agent_session_id);
CREATE INDEX idx_conversations_created_at ON conversations(created_at);
CREATE INDEX idx_conversations_sender ON conversations(sender);

-- ============================================
-- FUNCTIONS & TRIGGERS
-- ============================================

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Triggers for updated_at
CREATE TRIGGER update_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_mentorships_updated_at
    BEFORE UPDATE ON mentorships
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_agent_sessions_updated_at
    BEFORE UPDATE ON agent_sessions
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_agent_session_data_updated_at
    BEFORE UPDATE ON agent_session_data
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================
-- ROW LEVEL SECURITY (RLS)
-- ============================================
-- 
-- Este script habilita RLS em todas as tabelas e cria políticas
-- básicas de segurança. Como estamos usando ServiceRoleKey no backend,
-- o service_role bypassa RLS automaticamente, mas é boa prática ter
-- RLS habilitado para segurança e conformidade.
-- ============================================

-- 1. HABILITAR RLS EM TODAS AS TABELAS
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE mentorships ENABLE ROW LEVEL SECURITY;
ALTER TABLE agent_sessions ENABLE ROW LEVEL SECURITY;
ALTER TABLE agent_session_data ENABLE ROW LEVEL SECURITY;
ALTER TABLE conversations ENABLE ROW LEVEL SECURITY;

-- 2. POLÍTICAS PARA SERVICE ROLE
-- Service role (usado pelo backend) tem acesso total
-- Estas políticas são principalmente para documentação,
-- já que service_role bypassa RLS automaticamente

-- Users: Service role pode fazer tudo
CREATE POLICY "Service role has full access to users"
    ON users
    FOR ALL
    TO service_role
    USING (true)
    WITH CHECK (true);

-- Mentorships: Service role can do everything
CREATE POLICY "Service role has full access to mentorships"
    ON mentorships
    FOR ALL
    TO service_role
    USING (true)
    WITH CHECK (true);

-- Agent Sessions: Service role pode fazer tudo
CREATE POLICY "Service role has full access to agent_sessions"
    ON agent_sessions
    FOR ALL
    TO service_role
    USING (true)
    WITH CHECK (true);

-- Agent Session Data: Service role pode fazer tudo
CREATE POLICY "Service role has full access to agent_session_data"
    ON agent_session_data
    FOR ALL
    TO service_role
    USING (true)
    WITH CHECK (true);

-- Conversations: Service role pode fazer tudo
CREATE POLICY "Service role has full access to conversations"
    ON conversations
    FOR ALL
    TO service_role
    USING (true)
    WITH CHECK (true);

-- 3. POLÍTICAS PARA ANON/SERVICE (FUTURO)
-- Se no futuro você quiser permitir acesso via API pública
-- ou autenticação de usuários, pode criar políticas mais específicas
-- Por enquanto, deixamos apenas service_role

-- Exemplo de política futura (comentada):
-- CREATE POLICY "Users can read their own data"
--     ON users
--     FOR SELECT
--     TO authenticated
--     USING (auth.uid()::text = id::text);

-- ============================================
-- NOTAS IMPORTANTES SOBRE RLS:
-- ============================================
-- 1. Service Role Key bypassa RLS automaticamente
--    - Seu código continuará funcionando normalmente
--    - Estas políticas são principalmente para documentação e segurança
--
-- 2. Se você precisar de acesso público no futuro:
--    - Crie políticas específicas para 'anon' role
--    - Ou use 'authenticated' role com políticas baseadas em auth.uid()
--
-- 3. Para desabilitar RLS temporariamente (não recomendado):
--    ALTER TABLE users DISABLE ROW LEVEL SECURITY;
--
-- 4. Para remover uma política:
--    DROP POLICY "Service role has full access to users" ON users;
--
-- 5. Para verificar se RLS está habilitado:
--    SELECT tablename, rowsecurity 
--    FROM pg_tables 
--    WHERE schemaname = 'public' 
--      AND tablename IN ('users', 'mentorias', 'agent_sessions', 'agent_session_data', 'conversations');

-- ============================================
-- COMMENTS
-- ============================================

COMMENT ON TABLE users IS 'Physical persons identified by phone number';
COMMENT ON TABLE mentorships IS 'Mentorship programs created by mentors';
COMMENT ON TABLE agent_sessions IS 'Agent sessions - links User + Mentorship';
COMMENT ON TABLE agent_session_data IS 'Session data - common properties';
COMMENT ON TABLE conversations IS 'Message history per session';

COMMENT ON COLUMN users.phone_number IS 'Unique identifier, digits only (no +)';
COMMENT ON COLUMN mentorships.assistant_id IS 'OpenAI Assistant ID';
COMMENT ON COLUMN mentorships.evolution_api_key IS 'Evolution API key for this mentorship';
COMMENT ON COLUMN mentorships.evolution_instance_name IS 'Evolution API instance name for this mentorship';
COMMENT ON COLUMN agent_sessions.ai_context_id IS 'OpenAI Thread ID (persists indefinitely)';
COMMENT ON COLUMN agent_session_data.custom_properties_json IS 'Future custom properties (JSONB)';

-- ============================================
-- SAMPLE DATA (Optional - for testing)
-- ============================================

-- Uncomment to insert sample data for testing
/*
-- Sample mentor user
INSERT INTO users (phone_number, name, email, status) 
VALUES ('5511999999999', 'Paula', 'paula@example.com', 'Active');

-- Sample mentorship
INSERT INTO mentorships (name, mentor_id, assistant_id, duration_days, description, evolution_api_key, evolution_instance_name)
VALUES (
    'Nina - Mentorship Offer Discovery',
    (SELECT id FROM users WHERE phone_number = '5511999999999'),
    'asst_YOUR_ASSISTANT_ID_HERE',
    30,
    '30-day program to discover your unique mentorship offer',
    'YOUR_EVOLUTION_API_KEY',
    'YOUR_INSTANCE_NAME'
);
*/

-- ============================================
-- END OF SCHEMA
-- ============================================

