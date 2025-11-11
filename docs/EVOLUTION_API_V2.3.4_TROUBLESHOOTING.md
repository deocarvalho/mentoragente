# 🔧 Evolution API v2.3.4 - Render Free Tier Troubleshooting

## 🚨 Current Issue: Database Connection Failure

**Error:** `P1001: Can't reach database server at db.havdplzuqggkcpilhdif.supabase.co:5432`

**Root Cause:** Evolution API cannot connect to Supabase database during migration. The log shows "Database URL: " (empty), meaning the connection string isn't being read correctly.

## 🔧 IMMEDIATE FIX: Database Connection

### Issue 1: Supabase Firewall (Most Likely)

Supabase databases may block connections from Render's IP addresses. You need to allow connections from anywhere or whitelist Render IPs.

**Solution:**
1. Go to Supabase Dashboard → Your Project → Settings → Database
2. Find "Connection Pooling" or "Network Restrictions"
3. **Allow connections from any IP** (for testing) OR
4. Whitelist Render's IP ranges (check Render docs for current IPs)

### Issue 2: Connection String Format

Evolution API expects `DATABASE_CONNECTION_URI` in a specific format. Verify your connection string:

**Current format (from image):**
```
postgresql://postgres:M3n%2B0R%40g3N%2B3@db.havdplzuqggkcpilhdif.supabase.co:5432/postgres?sslmode=require
```

**Verify these environment variables are set correctly:**

```env
DATABASE_ENABLED=true
DATABASE_PROVIDER=postgresql
DATABASE_CONNECTION_URI=postgresql://postgres:M3n%2B0R%40g3N%2B3@db.havdplzuqggkcpilhdif.supabase.co:5432/postgres?sslmode=require
DATABASE_SSL=true
```

**Important Notes:**
- Password is URL-encoded: `M3n%2B0R%40g3N%2B3` = `M3n+0R@g3N+3`
- Must include `?sslmode=require` at the end
- Username is `postgres` (not your email)
- Database name is `postgres`

### Issue 3: Alternative Connection String Format

If the URL-encoded format doesn't work, try without encoding (but escape special characters):

```env
DATABASE_CONNECTION_URI=postgresql://postgres:M3n+0R@g3N+3@db.havdplzuqggkcpilhdif.supabase.co:5432/postgres?sslmode=require
```

**⚠️ Warning:** Special characters in passwords (`+`, `@`, `|`) may need different encoding. Try both formats.

### Issue 4: Supabase Connection Pooling

Supabase offers connection pooling. Try using the pooler port (6543) instead:

**⚠️ IMPORTANT:** For connection pooling, username format is different:
- Direct: `postgres`
- Pooled: `postgres.PROJECT_REF` (must include project reference)

```env
DATABASE_CONNECTION_URI=postgresql://postgres.havdplzuqggkcpilhdif:M3n%2B0R%40g3N%2B3@aws-0-us-west-1.pooler.supabase.com:6543/postgres?sslmode=require
```

**To get your pooler connection string:**
1. Go to Supabase Dashboard → Settings → Database
2. Find "Connection Pooling" section
3. Copy the "Connection string" (Session mode) - **it will have the correct username format**
4. Replace `[YOUR-PASSWORD]` with your actual password (URL-encoded)
5. Ensure `?sslmode=require` is at the end

**Error: "Tenant or user not found":**
- This means you used `postgres` instead of `postgres.havdplzuqggkcpilhdif`
- Fix: Use the format `postgres.PROJECT_REF` in the username

---

## 🚨 Previous Issue: "Failed deploy" Status

Based on your Render dashboard, the "Mentoragente Evolution API" service is showing "Failed deploy" status. Here's how to fix it:

## ✅ Step-by-Step Fix

### 1. Check Deployment Logs (FIRST STEP)

1. Go to Render Dashboard
2. Click on "Mentoragente Evolution API" service
3. Click "Logs" tab
4. Look for error messages - this will tell you exactly what's wrong

**Common errors you might see:**
- `SERVER_URL is required` → Missing `SERVER_URL` environment variable
- `Database connection failed` → Database connection issue
- `Port already in use` → Port configuration issue
- `Out of memory` → Free tier RAM limit exceeded

### 2. Verify Environment Variables

Go to your service → "Environment" tab and verify these are set:

#### ⚠️ CRITICAL - Must Have:

```env
SERVER_URL=https://mentoragente-evolution-api.onrender.com
```
**⚠️ This is the #1 cause of deployment failures!**
- Must match your Render service URL exactly
- Use HTTPS (not HTTP)
- No trailing slash

```env
PORT=8080
```

```env
DATABASE_ENABLED=true
DATABASE_PROVIDER=postgresql
DATABASE_CONNECTION_URI=postgresql://postgres:M3n%2B0R%40g3N%2B3@db.havdplzuqggkcpilhdif.supabase.co:5432/postgres?sslmode=require
DATABASE_SSL=true
```

```env
AUTHENTICATION_API_KEY=a86e9e9e26cacb92b73cd4892a7615fcc7e4f41624bf61cd0d56d65d2a94aa68
```

```env
NODE_ENV=production
```

#### Optional (but recommended):

```env
REDIS_ENABLED=false
```

### 3. Fix Health Check Settings

1. Go to service → "Settings" tab
2. Scroll to "Health Check" section
3. Try these settings:
   - **Health Check Path:** `/` (root path)
   - **Health Check Timeout:** `90` seconds
   - Or **disable health checks** temporarily to test

### 4. Verify Service Configuration

1. Go to service → "Settings" tab
2. Verify:
   - **Docker Image:** `evoapicloud/evolution-api:v2.3.4`
   - **Port:** `8080`
   - **Region:** Oregon (or your chosen region)

### 5. Manual Deploy

After fixing environment variables:

1. Go to service dashboard
2. Click "Manual Deploy" → "Deploy latest commit"
3. Monitor logs in real-time
4. Wait 2-3 minutes for deployment

## 🔍 Specific Issues & Solutions

### Issue: Missing SERVER_URL

**Symptom:** Service fails immediately, logs show "SERVER_URL is required"

**Solution:**
1. Go to Environment variables
2. Add: `SERVER_URL=https://mentoragente-evolution-api.onrender.com`
3. Replace with your actual Render service URL
4. Redeploy

### Issue: Health Check Failing

**Symptom:** Service starts but health check fails, status shows "Failed deploy"

**Solution:**
1. Try Health Check Path: `/`
2. Increase timeout to 90 seconds
3. Or disable health checks temporarily
4. Check Evolution API docs for correct health endpoint

### Issue: Database Connection Failed (P1001 Error)

**Symptom:** 
```
Error: P1001: Can't reach database server at `db.havdplzuqggkcpilhdif.supabase.co:5432`
Database URL: (empty)
```

**Solutions (try in order):**

1. **Check Supabase Firewall:**
   - Go to Supabase Dashboard → Settings → Database
   - Check "Network Restrictions" or "IP Allowlist"
   - **Allow connections from 0.0.0.0/0** (all IPs) for testing
   - Or whitelist Render's IP ranges

2. **Verify Connection String Format:**
   ```env
   DATABASE_CONNECTION_URI=postgresql://postgres:M3n%2B0R%40g3N%2B3@db.havdplzuqggkcpilhdif.supabase.co:5432/postgres?sslmode=require
   ```
   - Must include `?sslmode=require`
   - Password must be URL-encoded
   - Username is `postgres` (not email)

3. **Try Connection Pooling (Recommended):**
   - Use Supabase connection pooler (port 6543)
   - Get pooler connection string from Supabase Dashboard
   - Format: `postgresql://postgres.PROJECT_REF:PASSWORD@pooler.supabase.com:6543/postgres?sslmode=require`

4. **Test Connection String:**
   - Try connecting from your local machine first
   - Use psql or pgAdmin to verify credentials work
   - If local works but Render doesn't → firewall issue

5. **Check Environment Variable:**
   - Verify `DATABASE_CONNECTION_URI` is set (not `DATABASE_URL`)
   - Check for typos in variable name
   - Ensure no extra spaces or quotes

### Issue: Out of Memory

**Symptom:** Service crashes, logs show memory errors

**Solution:**
1. Free tier only has 512MB RAM
2. Evolution API v2.3.4 may need more
3. Options:
   - Upgrade to Starter plan ($7/month) for 1GB RAM
   - Try v2.1.1 which may be more memory-efficient
   - Optimize Evolution API configuration

### Issue: Port Already in Use

**Symptom:** Logs show port binding errors

**Solution:**
1. Ensure `PORT=8080` is set
2. Don't use other ports
3. Render uses port 8080 by default

## 📋 Quick Checklist

Before redeploying, verify:

- [ ] `SERVER_URL` is set and matches your Render URL
- [ ] `PORT=8080` is set
- [ ] `DATABASE_CONNECTION_URI` is correct and includes `?sslmode=require`
- [ ] `DATABASE_SSL=true` is set
- [ ] `AUTHENTICATION_API_KEY` is set
- [ ] `NODE_ENV=production` is set
- [ ] Health check timeout is increased to 90 seconds
- [ ] Docker image is `evoapicloud/evolution-api:v2.3.4`

## 🚀 After Successful Deployment

Once your service shows "Deployed" or "Live":

1. **Test the service:**
   ```bash
   curl https://mentoragente-evolution-api.onrender.com/
   ```

2. **Check health endpoint:**
   ```bash
   curl https://mentoragente-evolution-api.onrender.com/health
   ```

3. **Verify API is accessible:**
   - Service should respond to requests
   - Check logs for any runtime errors

## 💡 Free Tier Limitations

Remember, Render free tier has limitations:

- **512MB RAM** - May not be enough for Evolution API v2.3.4
- **Sleeps after 15 min inactivity** - First request after sleep takes 30-60 seconds
- **Limited CPU** - May struggle with concurrent connections
- **No Redis** - Evolution API can run without it, but performance may be reduced

**Recommendation:** For production, consider upgrading to Starter plan ($7/month) for:
- 1GB RAM (vs 512MB)
- No sleep (always-on)
- Better CPU performance

## 📚 Additional Resources

- [Evolution API v2.3.4 Documentation](https://doc.evolution-api.com/v2/)
- [Render Docker Deployment Guide](https://render.com/docs/docker)
- [Render Free Tier Limitations](https://render.com/docs/free)

---

**Last Updated:** January 2025

