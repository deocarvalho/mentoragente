# 🔍 Supabase Firewall Verification Checklist

## Current Issue: P1001 Connection Error

You're getting:
```
Error: P1001: Can't reach database server at `db.havdplzuqggkcpilhdif.supabase.co:5432`
```

This means Supabase is **blocking connections from Render**.

## ✅ Complete Verification Steps

### 1. Verify Supabase Project is Active

1. Go to https://app.supabase.com
2. Check if project **havdplzuqggkcpilhdif** is:
   - ✅ **Active** (green status)
   - ❌ **Paused** (if paused, click "Resume" or "Restore")

**Free tier projects can auto-pause after inactivity!**

### 2. Check Network Restrictions in Supabase

**Location:** Settings → Database → Network Restrictions

**What to look for:**
- Toggle: "Restrict connections" → Should be **OFF**
- IP Allowlist: Should be **empty** or contain `0.0.0.0/0`
- Any firewall rules blocking external IPs

**If you can't find this setting:**
- Some Supabase projects don't show explicit firewall settings
- This might mean connections are allowed by default
- Or it might be configured at a different level

### 3. Verify Connection String Format

**Current connection string (direct connection):**
```
postgresql://postgres:M3n%2B0R%40g3N%2B3@db.havdplzuqggkcpilhdif.supabase.co:5432/postgres?sslmode=require
```

**Verify each part:**
- ✅ Username: `postgres` (correct for direct connection)
- ✅ Password: `M3n%2B0R%40g3N%2B3` (URL-encoded: `M3n+0R@g3N+3`)
- ✅ Host: `db.havdplzuqggkcpilhdif.supabase.co` (with `db.` prefix)
- ✅ Port: `5432` (direct connection port)
- ✅ Database: `postgres`
- ✅ SSL: `?sslmode=require`

### 4. Test Connection Locally

**Test if connection string works from your machine:**

**Using PowerShell:**
```powershell
# Test DNS resolution
Resolve-DnsName db.havdplzuqggkcpilhdif.supabase.co

# Test port connectivity (if you have Test-NetConnection)
Test-NetConnection -ComputerName db.havdplzuqggkcpilhdif.supabase.co -Port 5432
```

**Using psql (if installed):**
```bash
psql "postgresql://postgres:M3n+0R@g3N+3@db.havdplzuqggkcpilhdif.supabase.co:5432/postgres?sslmode=require"
```

**If local connection works but Render doesn't:**
- ✅ Connection string is correct
- ✅ Database is accessible
- ❌ **Supabase is blocking Render's IP addresses** (firewall issue)

### 5. Check Supabase Connection Settings

**In Supabase Dashboard → Settings → Database:**

1. **Connection String:**
   - Copy the "Connection string" shown in Supabase
   - Compare with what you have in Render
   - Make sure password matches

2. **Connection Pooling:**
   - Note: Pooling doesn't work for migrations
   - But you can see if direct connection string is available

3. **Database Password:**
   - Verify password is correct
   - If unsure, reset database password in Supabase
   - Update connection string in Render with new password

### 6. Alternative: Use Supabase Connection String Directly

**Get the exact connection string from Supabase:**

1. Go to Supabase Dashboard → Settings → Database
2. Find "Connection string" section
3. Copy the **"URI"** format (not pooling)
4. It should look like:
   ```
   postgresql://postgres:[YOUR-PASSWORD]@db.havdplzuqggkcpilhdif.supabase.co:5432/postgres
   ```
5. Replace `[YOUR-PASSWORD]` with actual password (URL-encoded)
6. Add `?sslmode=require` at the end
7. Use this exact string in Render

### 7. Check Render Service Logs

**Look for more clues in Render logs:**

1. Go to Render Dashboard → Your Evolution API service → Logs
2. Look for:
   - DNS resolution errors
   - Timeout errors
   - Connection refused errors
   - Any other network-related errors

### 8. Verify Environment Variable in Render

**Double-check the variable is set correctly:**

1. Go to Render → Your service → Environment
2. Find `DATABASE_CONNECTION_URI`
3. Verify:
   - Variable name is exactly `DATABASE_CONNECTION_URI` (no typos)
   - Value doesn't have extra spaces or quotes
   - Value matches the format above exactly

### 9. Try Different Connection Approaches

**Option A: Reset Database Password**
1. Supabase Dashboard → Settings → Database
2. Click "Reset database password"
3. Copy new password
4. URL-encode it: `M3n+0R@g3N+3` → `M3n%2B0R%40g3N%2B3`
5. Update connection string in Render

**Option B: Check if Project Region Matters**
- Supabase projects have regions
- Render services have regions
- Try to match regions if possible (Oregon for both)

**Option C: Contact Supabase Support**
- If nothing works, contact Supabase support
- Ask: "Is my project allowing external connections from Render?"
- Provide your project reference: `havdplzuqggkcpilhdif`

## 🎯 Most Likely Solutions (in order)

1. **Supabase project is paused** → Resume it
2. **Network restrictions enabled** → Disable them
3. **Wrong connection string format** → Use exact format from Supabase dashboard
4. **Password encoding issue** → Reset password and re-encode
5. **Render IP blocked** → Contact Supabase to whitelist Render IPs

## 📋 Quick Test Checklist

- [ ] Supabase project is Active (not paused)
- [ ] Network restrictions are disabled
- [ ] Connection string format is correct
- [ ] Password is URL-encoded correctly
- [ ] Tested connection locally (if possible)
- [ ] Environment variable name is correct in Render
- [ ] No extra spaces/quotes in connection string
- [ ] Waited 1-2 minutes after Supabase changes
- [ ] Redeployed service after changes

---

**Last Updated:** January 2025

