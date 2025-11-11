# 🔥 Supabase Firewall Fix for Render Deployment

## 🚨 Problem

Evolution API on Render cannot connect to Supabase database:
```
Error: P1001: Can't reach database server at `db.havdplzuqggkcpilhdif.supabase.co:5432`
```

## ✅ Solution: Configure Supabase Network Access

### Step 1: Check Supabase Network Settings

1. Go to [Supabase Dashboard](https://app.supabase.com)
2. Select your project: **havdplzuqggkcpilhdif** (Mentoragente)
3. Go to **Settings** → **Database**
4. Scroll to **Network Restrictions** or **IP Allowlist** section

### Step 2: Allow Connections from Render

**Option A: Allow All IPs (Easiest for Testing)**

1. Find "Network Restrictions" or "IP Allowlist"
2. If there's a toggle for "Restrict connections", **turn it OFF**
3. Or add `0.0.0.0/0` to allow all IPs
4. Save changes

**Option B: Whitelist Render IPs (More Secure)**

Render's IP addresses change, but you can:
1. Check Render's documentation for current IP ranges
2. Add those IP ranges to Supabase allowlist
3. Or use Supabase's "Allow connections from anywhere" option

### Step 3: Verify Connection String

Make sure your `DATABASE_CONNECTION_URI` in Render is correct:

```env
DATABASE_CONNECTION_URI=postgresql://postgres:M3n%2B0R%40g3N%2B3@db.havdplzuqggkcpilhdif.supabase.co:5432/postgres?sslmode=require
```

**Key points:**
- Username: `postgres` (not your email)
- Password: URL-encoded (`M3n%2B0R%40g3N%2B3` = `M3n+0R@g3N+3`)
- Must include `?sslmode=require`
- Host: `db.havdplzuqggkcpilhdif.supabase.co` (with `db.` prefix)

### Step 4: Use Connection Pooling (Recommended)

Supabase connection pooling is more reliable and handles connections better:

1. Go to Supabase Dashboard → Settings → Database
2. Find **Connection Pooling** section
3. Copy the **Connection string** (Session mode)
4. It will look like:
   ```
   postgresql://postgres.PROJECT_REF:PASSWORD@aws-0-us-west-1.pooler.supabase.com:6543/postgres
   ```
5. Replace `PASSWORD` with your actual password (URL-encoded)
6. Add `?sslmode=require` at the end
7. Update `DATABASE_CONNECTION_URI` in Render with this new string

**Example (CORRECT FORMAT):**
```env
DATABASE_CONNECTION_URI=postgresql://postgres.havdplzuqggkcpilhdif:M3n%2B0R%40g3N%2B3@aws-0-us-west-1.pooler.supabase.com:6543/postgres?sslmode=require
```

**⚠️ CRITICAL:** The username format for connection pooling is:
- ❌ **WRONG:** `postgres` (direct connection format)
- ✅ **CORRECT:** `postgres.havdplzuqggkcpilhdif` (pooled connection format)

The format is: `postgres.PROJECT_REF` where `PROJECT_REF` is your Supabase project reference ID (`havdplzuqggkcpilhdif`).

### Step 5: Redeploy on Render

1. After updating Supabase settings, wait 1-2 minutes
2. Go to Render Dashboard → Your Evolution API service
3. Click **Manual Deploy** → **Deploy latest commit**
4. Monitor logs to see if connection succeeds

## 🔍 Alternative: Test Connection Locally

To verify your connection string works:

**Using psql (if installed):**
```bash
psql "postgresql://postgres:M3n+0R@g3N+3@db.havdplzuqggkcpilhdif.supabase.co:5432/postgres?sslmode=require"
```

**Using pgAdmin or DBeaver:**
- Create new connection
- Use the connection string (decode password)
- Test connection

If it works locally but not on Render → **Firewall issue** (most likely)

## 📋 Checklist

- [x] Supabase network restrictions disabled or Render IPs whitelisted
- [x] Connection string format is correct
- [x] Password is URL-encoded in connection string
- [x] `?sslmode=require` is included
- [x] Username is `postgres` (not email)
- [ ] Tried connection pooling (port 6543)
- [ ] Redeployed service after changes
- [ ] Checked logs for new errors

## 🔧 Error: "Tenant or user not found" (Connection Pooling)

**Symptom:** 
```
Error: Schema engine error:
FATAL: Tenant or user not found
```

**Cause:** Wrong username format for connection pooling. The pooler requires `postgres.PROJECT_REF` format, not just `postgres`.

**Solution:**
1. Check your connection string format
2. Username must be: `postgres.havdplzuqggkcpilhdif` (not just `postgres`)
3. Format: `postgresql://postgres.PROJECT_REF:PASSWORD@pooler.supabase.com:6543/postgres?sslmode=require`

**Correct Example:**
```env
DATABASE_CONNECTION_URI=postgresql://postgres.havdplzuqggkcpilhdif:M3n%2B0R%40g3N%2B3@aws-0-us-west-1.pooler.supabase.com:6543/postgres?sslmode=require
```

**To get the exact format:**
1. Go to Supabase Dashboard → Settings → Database
2. Find "Connection Pooling" section
3. Copy the "Connection string" (Session mode) - it will have the correct username format
4. Replace `[YOUR-PASSWORD]` with your URL-encoded password
5. Ensure `?sslmode=require` is at the end

**⚠️ IMPORTANT:** Prisma migrations don't work through connection pooling. Use direct connection for Evolution API deployments.

---

## 🔄 Back to Connection Error (P1001)

If you switched back to direct connection and got the original error again:

```
Error: P1001: Can't reach database server at `db.havdplzuqggkcpilhdif.supabase.co:5432`
```

This means **Supabase firewall is blocking Render's IP addresses**. You MUST configure Supabase to allow connections.

### Step-by-Step Supabase Firewall Configuration:

1. **Go to Supabase Dashboard:**
   - https://app.supabase.com
   - Select project: **havdplzuqggkcpilhdif**

2. **Navigate to Database Settings:**
   - Click **Settings** (gear icon in left sidebar)
   - Click **Database** in the settings menu

3. **Find Network Restrictions:**
   - Scroll down to find **"Network Restrictions"** or **"IP Allowlist"** section
   - Look for any toggle or setting that restricts connections

4. **Allow All Connections (for testing):**
   - If there's a toggle "Restrict connections" or "IP Allowlist enabled" → **Turn it OFF**
   - Or if there's an allowlist, add `0.0.0.0/0` to allow all IPs
   - **Save changes**

5. **Verify Connection String:**
   ```env
   DATABASE_CONNECTION_URI=postgresql://postgres:M3n%2B0R%40g3N%2B3@db.havdplzuqggkcpilhdif.supabase.co:5432/postgres?sslmode=require
   ```
   - Username: `postgres` (not `postgres.havdplzuqggkcpilhdif`)
   - Host: `db.havdplzuqggkcpilhdif.supabase.co` (with `db.` prefix)
   - Port: `5432`
   - Must include `?sslmode=require`

6. **Wait 1-2 minutes** after changing Supabase settings (propagation time)

7. **Redeploy on Render:**
   - Go to Render Dashboard → Your Evolution API service
   - Click **Manual Deploy** → **Deploy latest commit**
   - Monitor logs

### If You Can't Find Network Restrictions:

Some Supabase projects don't have explicit firewall settings visible. In that case:

1. **Check Supabase Project Status:**
   - Ensure project is **Active** (not paused)
   - Free tier projects can be paused after inactivity

2. **Verify Database is Running:**
   - Go to Supabase Dashboard → Database
   - Check if you can see tables/data
   - If project is paused, you'll need to resume it

3. **Test Connection Locally:**
   - Try connecting from your local machine using the same connection string
   - If local works but Render doesn't → **Definitely a firewall/network issue**

4. **Contact Supabase Support:**
   - If you can't find network settings, contact Supabase support
   - Ask them to verify if your project allows external connections
   - Provide your Render service IP (if you can find it)

---

## 🚨 Still Not Working?

If connection still fails after these steps:

1. **Check Supabase Project Status:**
   - Ensure project is active (not paused)
   - Check if database is running

2. **Verify Database Credentials:**
   - Go to Supabase Dashboard → Settings → Database
   - Check if password is correct
   - Reset password if needed

3. **Check Render Logs:**
   - Look for more specific error messages
   - Check if DNS resolution works
   - Verify port 5432 is accessible

4. **Try Direct Connection (bypass pooler):**
   - Use direct connection: `db.havdplzuqggkcpilhdif.supabase.co:5432`
   - Instead of pooler: `pooler.supabase.com:6543`

5. **Contact Support:**
   - Supabase support if database issues
   - Render support if network issues

---

**Last Updated:** January 2025

