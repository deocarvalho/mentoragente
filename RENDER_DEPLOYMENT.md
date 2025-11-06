# 🚀 Render Deployment Guide - Mentoragente

## Evolution API Deployment on Render

### Recommended Version: **v2.1.1** (Latest Stable)

### 1. Deploy Evolution API as a Web Service

**Service Type:** Web Service  
**Docker Image:** `atendai/evolution-api:v2.1.1`

#### Environment Variables for Evolution API:

```env
# Required
SERVER_URL=https://your-evolution-api.onrender.com
PORT=8080

# Database (PostgreSQL recommended for production)
DATABASE_ENABLED=true
DATABASE_PROVIDER=postgresql
DATABASE_CONNECTION_URI=postgresql://user:password@host:5432/dbname

# Redis (recommended for production)
REDIS_ENABLED=true
REDIS_URI=redis://host:6379

# Security
API_KEY=your-secure-api-key-here
CONFIG_SESSION_PHONE_CLIENT=Chrome
CONFIG_SESSION_PHONE_NAME=Evolution API

# Webhook (your Mentoragente API)
WEBHOOK_GLOBAL_ENABLED=true
WEBHOOK_GLOBAL_URL=https://your-mentoragente-api.onrender.com/api/webhook
WEBHOOK_GLOBAL_WEBHOOK_BY_EVENTS=true
```

#### Render Configuration:

- **Name:** `evolution-api` (or your preferred name)
- **Region:** Choose closest to your users
- **Instance Type:** Free tier works for testing, but consider paid for production
- **Health Check Path:** `/health` or `/`
- **Auto-Deploy:** Enable if using GitHub

### 2. Deploy Mentoragente API

**Service Type:** Web Service  
**Dockerfile Path:** `Mentoragente.API/Dockerfile`

#### Environment Variables for Mentoragente API:

```env
# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
PORT=8080

# OpenAI
OpenAI__ApiKey=sk-proj-your-key-here
OpenAI__BaseUrl=https://api.openai.com/v1
OpenAI__AssistantId=asst_your-assistant-id

# Evolution API (point to your Evolution API service)
EvolutionAPI__BaseUrl=https://your-evolution-api.onrender.com

# Supabase
Supabase__Url=https://your-project.supabase.co
Supabase__ServiceRoleKey=your-service-role-key
Supabase__AnonKey=your-anon-key

# API Key Authentication (optional)
ApiKey__Key=your-api-key-for-authentication
```

#### Render Configuration:

- **Name:** `mentoragente-api`
- **Region:** Same as Evolution API (for lower latency)
- **Instance Type:** Free tier for testing, paid for production
- **Health Check Path:** `/health`
- **Auto-Deploy:** Enable

### 3. Database Setup

#### Option A: Use Supabase (Recommended)
- Already configured in your app
- Managed PostgreSQL with RLS
- Free tier available

#### Option B: Render PostgreSQL
- Create PostgreSQL database on Render
- Update `Supabase__Url` and connection strings
- Run `DATABASE_SCHEMA.sql` migration

### 4. Redis Setup (Optional but Recommended)

For Evolution API production use, Redis helps with:
- Session management
- Message queue
- Rate limiting

**Service Type:** Redis  
**Plan:** Free tier for testing

Update Evolution API env vars:
```env
REDIS_ENABLED=true
REDIS_URI=redis://your-redis.onrender.com:6379
```

## 🔧 Configuration Best Practices

### Evolution API v2.1.1 Features:

1. **Multi-Instance Support** ✅
   - Each mentorship can have its own instance
   - Configure via `evolution_instance_name` in database

2. **Webhook Configuration** ✅
   - Global webhook: All instances
   - Per-instance webhook: Specific instances
   - Your current implementation supports this

3. **API Key Authentication** ✅
   - Store API keys per mentorship in database
   - More secure than global API key

### Recommended Render Services:

1. **Evolution API** (Web Service)
   - Docker: `atendai/evolution-api:v2.1.1`
   - Instance: Starter ($7/month) or higher for production

2. **Mentoragente API** (Web Service)
   - Docker: Your Dockerfile
   - Instance: Free tier for testing, Starter for production

3. **PostgreSQL** (Database)
   - Use Supabase (recommended) or Render PostgreSQL
   - Free tier available

4. **Redis** (Optional)
   - For Evolution API session management
   - Free tier available

## 📝 Deployment Steps

### Step 1: Deploy Evolution API

1. Go to Render Dashboard
2. Click "New +" → "Web Service"
3. Connect your GitHub repo (or use public image)
4. **For Docker Image:**
   - Select "Docker"
   - Image: `atendai/evolution-api:v2.1.1`
5. Configure environment variables (see above)
6. Deploy

### Step 2: Deploy Mentoragente API

1. Go to Render Dashboard
2. Click "New +" → "Web Service"
3. Connect your GitHub repo: `deocarvalho/mentoragente`
4. **Build Settings:**
   - Root Directory: `source/Mentoragente`
   - Dockerfile Path: `Mentoragente.API/Dockerfile`
5. Configure environment variables (see above)
6. Set `EvolutionAPI__BaseUrl` to your Evolution API URL
7. Deploy

### Step 3: Configure Webhooks

1. In Evolution API dashboard, configure webhook:
   ```
   https://your-mentoragente-api.onrender.com/api/webhook?mentorshipId={MENTORSHIP_ID}
   ```

2. Or use Evolution API's webhook configuration API:
   ```bash
   curl -X POST https://your-evolution-api.onrender.com/webhook/set/YOUR_INSTANCE_NAME \
     -H "apikey: YOUR_API_KEY" \
     -H "Content-Type: application/json" \
     -d '{
       "url": "https://your-mentoragente-api.onrender.com/api/webhook?mentorshipId=YOUR_MENTORSHIP_ID",
       "webhook_by_events": true,
       "events": ["messages.upsert"]
     }'
   ```

## 🔒 Security Recommendations

1. **Use Environment Variables** (never commit secrets)
2. **Enable HTTPS** (Render provides automatically)
3. **Use Strong API Keys** (generate secure random strings)
4. **Enable RLS** in Supabase (already configured)
5. **Rate Limiting** (consider adding to your API)

## 📊 Monitoring

### Health Checks:

- **Evolution API:** `https://your-evolution-api.onrender.com/health`
- **Mentoragente API:** `https://your-mentoragente-api.onrender.com/health`

### Logs:

- View logs in Render Dashboard
- Both services log to stdout/stderr
- Use Render's log aggregation

## 🚨 Troubleshooting

### Evolution API Issues:

1. **Instance not connecting:**
   - Check Redis connection (if enabled)
   - Verify database connection
   - Check instance name matches

2. **Webhook not receiving:**
   - Verify webhook URL is accessible
   - Check webhook configuration in Evolution API
   - Verify mentorship ID in query parameter

### Mentoragente API Issues:

1. **Cannot connect to Evolution API:**
   - Verify `EvolutionAPI__BaseUrl` is correct
   - Check Evolution API is running
   - Verify API key in database

2. **Database connection issues:**
   - Verify Supabase credentials
   - Check RLS policies (ServiceRoleKey bypasses RLS)
   - Verify network connectivity

## 📚 Resources

- [Evolution API Documentation](https://doc.evolution-api.com/v2/)
- [Evolution API v2.1.1 Release Notes](https://doc.evolution-api.com/v2/en/updates)
- [Render Documentation](https://render.com/docs)
- [Render Docker Guide](https://render.com/docs/docker)

---

**Last Updated:** November 2025  
**Evolution API Version:** v2.1.1 (Recommended)

