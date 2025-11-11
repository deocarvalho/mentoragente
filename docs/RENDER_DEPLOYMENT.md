# 🚀 Render Deployment Guide - Mentoragente

## Evolution API Deployment on Render

### Evolution API v2.3.4 Deployment (Current)

### 1. Deploy Evolution API as a Web Service

**Service Type:** Web Service  
**Docker Image:** `evoapicloud/evolution-api:v2.3.4`

#### ⚠️ CRITICAL: Required Environment Variables for v2.3.4

**The most common deployment failure is missing `SERVER_URL`. Make sure it's set!**

```env
# ⚠️ REQUIRED - Missing this causes deployment failure
SERVER_URL=https://your-evolution-api.onrender.com
PORT=8080

# Database (PostgreSQL - Required)
DATABASE_ENABLED=true
DATABASE_PROVIDER=postgresql
DATABASE_CONNECTION_URI=postgresql://user:password@host:5432/dbname?sslmode=require
DATABASE_SSL=true

# Authentication (v2.3.4 uses AUTHENTICATION_API_KEY)
AUTHENTICATION_API_KEY=your-secure-api-key-here

# Node Environment
NODE_ENV=production

# Redis (Optional but recommended for production)
# Note: Render free tier doesn't include Redis, but Evolution API can run without it
REDIS_ENABLED=false
# REDIS_URI=redis://host:6379  # Only if you have Redis

# Session Configuration (Optional)
CONFIG_SESSION_PHONE_CLIENT=Chrome
CONFIG_SESSION_PHONE_NAME=Evolution API

# Webhook (your Mentoragente API - Optional, configure later)
WEBHOOK_GLOBAL_ENABLED=true
WEBHOOK_GLOBAL_URL=https://your-mentoragente-api.onrender.com/api/webhook
WEBHOOK_GLOBAL_WEBHOOK_BY_EVENTS=true
```

#### Render Configuration for v2.3.4:

- **Name:** `Mentoragente Evolution API` (or your preferred name)
- **Region:** Oregon (or closest to your users)
- **Instance Type:** Free tier (512MB RAM, can be limiting)
- **Docker Image:** `evoapicloud/evolution-api:v2.3.4`
- **Health Check Path:** `/` or `/health` (check Evolution API docs for exact path)
- **Health Check Timeout:** Increase to 60-90 seconds (free tier services start slower)
- **Auto-Deploy:** Disable initially (deploy manually until stable)

#### ⚠️ Free Tier Limitations:

1. **512MB RAM** - Evolution API can be memory-intensive
2. **No Redis** - Evolution API can run without Redis, but performance may be reduced
3. **Sleep after inactivity** - Service sleeps after 15 minutes of inactivity
4. **Slower startup** - First request after sleep can take 30-60 seconds
5. **Limited CPU** - May struggle with multiple concurrent connections

#### 🔧 Troubleshooting Failed Deployments:

**1. Check Deployment Logs:**
   - Go to Render Dashboard → Your Service → Logs
   - Look for errors during startup
   - Common errors:
     - "SERVER_URL is required" → Add `SERVER_URL` environment variable
     - "Database connection failed" → Check `DATABASE_CONNECTION_URI`
     - "Port already in use" → Ensure `PORT=8080` matches Render's port

**2. Verify Environment Variables:**
   - ✅ `SERVER_URL` - **MUST match your Render service URL**
   - ✅ `PORT=8080` - Must be 8080 for Render
   - ✅ `DATABASE_CONNECTION_URI` - Full PostgreSQL connection string
   - ✅ `AUTHENTICATION_API_KEY` - Secure random string
   - ✅ `NODE_ENV=production`

**3. Health Check Issues:**
   - Evolution API v2.3.4 may use `/` or `/health` endpoint
   - Try setting Health Check Path to `/` first
   - Increase Health Check Timeout to 90 seconds
   - Disable health checks temporarily to test

**4. Memory Issues (Free Tier):**
   - If service crashes, it's likely memory-related
   - Consider upgrading to Starter plan ($7/month) for 512MB → 1GB RAM
   - Or optimize Evolution API configuration

**5. Database Connection:**
   - Ensure `DATABASE_SSL=true` for Supabase connections
   - Connection string must include `?sslmode=require`
   - Test connection string format: `postgresql://user:pass@host:5432/db?sslmode=require`

---

### Evolution API v2.1.1 Deployment (Alternative)

**Service Type:** Web Service  
**Docker Image:** `atendai/evolution-api:v2.1.1`

#### Environment Variables for v2.1.1:

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

# Security (v2.1.1 uses API_KEY)
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

## 📝 Deployment Steps for v2.3.4

### Step 1: Deploy Evolution API v2.3.4

1. Go to Render Dashboard
2. Click "New +" → "Web Service"
3. **For Docker Image Deployment:**
   - Select "Docker" as the environment
   - **Docker Image:** `evoapicloud/evolution-api:v2.3.4`
   - **Name:** `Mentoragente Evolution API` (or your preferred name)
   - **Region:** Oregon (or closest to your users)
   - **Instance Type:** Free (or Starter for production)

4. **Configure Environment Variables (CRITICAL):**
   - Click "Environment" tab
   - Add ALL required variables (see above)
   - **⚠️ MUST include `SERVER_URL`** - Set this to your Render service URL
   - Example: `SERVER_URL=https://mentoragente-evolution-api.onrender.com`

5. **Health Check Settings:**
   - Health Check Path: `/` (try this first)
   - Health Check Timeout: `90` seconds
   - Or disable health checks temporarily

6. **Deploy:**
   - Click "Create Web Service"
   - Monitor logs for errors
   - Wait 2-3 minutes for first deployment
   - Check if service status changes to "Live" or "Deployed"

7. **If Deployment Fails:**
   - Check logs immediately
   - Verify `SERVER_URL` is set correctly
   - Verify database connection string
   - Try increasing health check timeout
   - Try disabling health checks temporarily

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

### Evolution API v2.3.4 Deployment Failures:

**Most Common Issues:**

1. **"Failed deploy" Status:**
   - ✅ **Check `SERVER_URL` is set** - This is the #1 cause of failures
   - ✅ Verify `SERVER_URL` matches your Render service URL exactly
   - ✅ Check deployment logs for specific error messages
   - ✅ Ensure `PORT=8080` is set

2. **Health Check Failures:**
   - Try Health Check Path: `/` (root)
   - Try Health Check Path: `/health`
   - Increase Health Check Timeout to 90 seconds
   - Disable health checks temporarily to test if service starts

3. **Database Connection Errors:**
   - Verify `DATABASE_CONNECTION_URI` format is correct
   - Ensure `DATABASE_SSL=true` for Supabase
   - Connection string must include `?sslmode=require`
   - Test database connection from Render logs

4. **Memory Issues (Free Tier):**
   - Evolution API v2.3.4 may need more than 512MB RAM
   - Check logs for "out of memory" errors
   - Consider upgrading to Starter plan ($7/month)
   - Or try v2.1.1 which may be more memory-efficient

5. **Startup Timeout:**
   - Free tier services start slower
   - Increase health check timeout
   - Wait 2-3 minutes for first deployment
   - Check logs to see if service is actually starting

6. **Port Configuration:**
   - Must use `PORT=8080` (Render's default)
   - Don't use other ports
   - Verify in Render service settings

### Evolution API General Issues:

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

---

## 📋 Quick Reference: v2.3.4 vs v2.1.1

| Feature | v2.3.4 | v2.1.1 |
|---------|--------|--------|
| Docker Image | `evoapicloud/evolution-api:v2.3.4` | `atendai/evolution-api:v2.1.1` |
| API Key Variable | `AUTHENTICATION_API_KEY` | `API_KEY` |
| Latest Features | ✅ Yes | ⚠️ Older |
| Free Tier Compatible | ⚠️ May need more RAM | ✅ Better |
| Recommended For | Production (paid tier) | Free tier testing |

---

**Last Updated:** January 2025  
**Evolution API Version:** v2.3.4 (Current) | v2.1.1 (Alternative)

