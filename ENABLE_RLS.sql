-- ============================================
-- ENABLE ROW LEVEL SECURITY (RLS)
-- Mentoragente Database - Supabase
-- ============================================
-- 
-- Este script habilita RLS em todas as tabelas e cria políticas
-- básicas de segurança. Como estamos usando ServiceRoleKey no backend,
-- o service_role bypassa RLS automaticamente, mas é boa prática ter
-- RLS habilitado para segurança e conformidade.
-- ============================================

-- ============================================
-- 1. HABILITAR RLS EM TODAS AS TABELAS
-- ============================================

ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE mentorias ENABLE ROW LEVEL SECURITY;
ALTER TABLE agent_sessions ENABLE ROW LEVEL SECURITY;
ALTER TABLE agent_session_data ENABLE ROW LEVEL SECURITY;
ALTER TABLE conversations ENABLE ROW LEVEL SECURITY;

-- ============================================
-- 2. POLÍTICAS PARA SERVICE ROLE
-- ============================================
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

-- Mentorias: Service role pode fazer tudo
CREATE POLICY "Service role has full access to mentorias"
    ON mentorias
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

-- ============================================
-- 3. POLÍTICAS PARA ANON/SERVICE (FUTURO)
-- ============================================
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
-- 4. VERIFICAÇÃO
-- ============================================
-- Para verificar se RLS está habilitado:
-- SELECT tablename, rowsecurity 
-- FROM pg_tables 
-- WHERE schemaname = 'public' 
--   AND tablename IN ('users', 'mentorias', 'agent_sessions', 'agent_session_data', 'conversations');

-- ============================================
-- NOTAS IMPORTANTES:
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
-- ============================================
-- FIM DO SCRIPT
-- ============================================

