# Zoom Server-to-Server OAuth Setup Guide

This guide explains how to set up Zoom Server-to-Server OAuth to automatically create meetings via API.

## Prerequisites

- A Zoom account (Pro, Business, or Enterprise)
- Access to Zoom Marketplace: https://marketplace.zoom.us/

## Step 1: Create a Server-to-Server OAuth App

1. Go to https://marketplace.zoom.us/
2. Sign in with your Zoom account
3. Click **"Develop"** → **"Build App"**
4. Choose **"Server-to-Server OAuth"**
5. Click **"Create"**

## Step 2: Configure Your App

### App Information
- **App Name:** `ApprendsMoi Meeting Creator` (or any name you prefer)
- **Short Description:** `Automatically creates Zoom meetings for ApprendsMoi`
- **Company Name:** Your company name
- **Developer Contact:** Your email

Click **Continue**

### App Credentials

You'll see three important credentials:

1. **Account ID** - Copy this
2. **Client ID** - Copy this  
3. **Client Secret** - Copy this (click "View" to reveal)

**Keep these secure!** You'll need them for the `.env` file.

Click **Continue**

### Scopes

Add the following scopes (permissions):

**Essential scopes:**
- `meeting:write:admin` - Create meetings
- `meeting:read:admin` - Read meeting details

To add scopes:
1. Click **"+ Add Scopes"**
2. Search for "meeting"
3. Select the scopes above
4. Click **"Done"**
5. Click **Continue**

### Activation

1. Review your app settings
2. Click **"Activate your app"**
3. Your app is now ready to use!

## Step 3: Get Your Meeting SDK Credentials

You already have these from the Meeting SDK app, but if you need them:

1. Go to https://marketplace.zoom.us/
2. Click **"Develop"** → **"Build App"**
3. Choose **"Meeting SDK"**
4. Create or select your existing Meeting SDK app
5. Copy:
   - **SDK Key**
   - **SDK Secret**

## Step 4: Configure Your Backend

### Option 1: Using `.env` file (Recommended for development)

1. Copy `.env.example` to `.env`:
   ```bash
   cp backend/.env.example backend/.env
   ```

2. Edit `backend/.env` and add your credentials:
   ```env
   # Zoom Server-to-Server OAuth (for creating meetings)
   ZOOM_ACCOUNT_ID=your_account_id_here
   ZOOM_CLIENT_ID=your_client_id_here
   ZOOM_CLIENT_SECRET=your_client_secret_here

   # Zoom Meeting SDK (for embedding meetings)
   ZOOM_SDK_KEY=your_sdk_key_here
   ZOOM_SDK_SECRET=your_sdk_secret_here
   ```

### Option 2: Using `appsettings.json` (Alternative)

Edit `backend/appsettings.json`:

```json
{
  "Zoom": {
    "AccountId": "your_account_id",
    "ClientId": "your_client_id",
    "ClientSecret": "your_client_secret",
    "SdkKey": "your_sdk_key",
    "SdkSecret": "your_sdk_secret"
  }
}
```

**Note:** Never commit `appsettings.json` with real credentials to Git!

## Step 5: Test the Setup

1. **Start the backend:**
   ```bash
   cd backend
   dotnet run
   ```

2. **Start the frontend:**
   ```bash
   cd frontend
   ng serve
   ```

3. **Open the browser:**
   - Navigate to `http://localhost:4200/visio`
   - A meeting should automatically be created
   - The Zoom SDK should load the meeting

## How It Works

### Backend Flow

1. **User arrives on `/visio` page**
2. **Frontend calls** `POST /api/zoom/meeting`
3. **Backend:**
   - Gets OAuth token using Account ID, Client ID, and Client Secret
   - Calls Zoom API to create an instant meeting
   - Generates SDK signature for the meeting
   - Returns meeting details (ID, password, join URL, signature)
4. **Frontend:**
   - Receives meeting details
   - Initializes Zoom SDK with the signature
   - Displays the meeting in the embedded container

### API Endpoints

**Create Meeting:**
```http
POST http://localhost:5254/api/zoom/meeting
Content-Type: application/json

{
  "topic": "ApprendsMoi - Session de classe"
}
```

**Response:**
```json
{
  "meetingId": 86098037935,
  "meetingNumber": "86098037935",
  "topic": "ApprendsMoi - Session de classe",
  "joinUrl": "https://zoom.us/j/86098037935?pwd=...",
  "password": "abc123",
  "signature": "eyJhbGc...",
  "sdkKey": "your_sdk_key"
}
```

## Troubleshooting

### Error: "Configuration Zoom manquante"

**Cause:** Missing Account ID, Client ID, or Client Secret

**Solution:** 
- Verify all three credentials are in your `.env` file
- Restart the backend after updating `.env`
- Check the credentials are correct (no extra spaces)

### Error: "Impossible d'obtenir le token d'accès Zoom"

**Cause:** Invalid credentials or app not activated

**Solution:**
- Verify your credentials match those in Zoom Marketplace
- Ensure your Server-to-Server OAuth app is activated
- Check your app hasn't been deactivated

### Error: "Erreur lors de la création de la réunion"

**Cause:** Missing scopes or API error

**Solution:**
- Verify you added `meeting:write:admin` and `meeting:read:admin` scopes
- Check your Zoom account has API access enabled
- Review backend logs for detailed error message

### Meeting creates but SDK doesn't load

**Cause:** SDK credentials missing or incorrect

**Solution:**
- Verify `ZOOM_SDK_KEY` and `ZOOM_SDK_SECRET` are set
- These are different from the OAuth credentials
- Check the Meeting SDK app is active

## Security Best Practices

1. **Never commit credentials to Git**
   - Add `.env` to `.gitignore`
   - Use environment variables in production

2. **Use environment variables in production**
   - Set via hosting platform (Azure, AWS, etc.)
   - Don't use `appsettings.json` for secrets

3. **Rotate credentials regularly**
   - Generate new Client Secret periodically
   - Update in all environments

4. **Limit scope permissions**
   - Only request scopes you actually need
   - Remove unused scopes

## Production Deployment

For production, set environment variables via your hosting platform:

**Azure App Service:**
```bash
az webapp config appsettings set --name your-app-name --resource-group your-rg \
  --settings ZOOM_ACCOUNT_ID=xxx ZOOM_CLIENT_ID=xxx ZOOM_CLIENT_SECRET=xxx \
  ZOOM_SDK_KEY=xxx ZOOM_SDK_SECRET=xxx
```

**Docker:**
```bash
docker run -e ZOOM_ACCOUNT_ID=xxx -e ZOOM_CLIENT_ID=xxx ...
```

## Additional Resources

- [Zoom Server-to-Server OAuth Documentation](https://developers.zoom.us/docs/internal-apps/s2s-oauth/)
- [Zoom Meeting API Reference](https://developers.zoom.us/docs/api/rest/reference/zoom-api/methods/#operation/meetingCreate)
- [Zoom Meeting SDK Documentation](https://developers.zoom.us/docs/meeting-sdk/web/)
