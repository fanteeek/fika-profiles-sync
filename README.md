# 🔄 Fika Profiles Sync

Automated profile synchronization tool for **SPT Tarkov** (FIKA Project). This tool allows you and your friends to keep your progress synced by storing profiles in a private GitHub repository.

## 📋 Prerequisites
Before running the tool, you need to set up a place to store the profiles:

1. **Create a GitHub Repository**
   * Go to GitHub and create a new private repository.

2. **Generate a Token**
   * Go to [Developer Settings > Personal Access Tokens > Fine-grained tokens](https://github.com/settings/tokens?type=beta).
   * Click **Generate new token**.
   * Select your new repository under "Repository access".
   * **Permissions:** Expand "Repository permissions" and set **Contents** to **Read and Write**.

## 🛠️ Installation & Usage
1. **Download** the latest `FikaSync.exe` from the Releases page.
2. **Place** the executable in your main EFT folder (the same folder containing `EscapeFromTarkov.exe`).
3. **Run** `FikaSync.exe`.
4. **Configure** on first run:
   * **Token:** Paste your Fine-grained PAT.
   * **URL:** Paste the link to your repository (e.g., `https://github.com/username/my-fika-repo`).

The launcher will now automatically sync your profiles before starting the server and push changes after you finish playing.
