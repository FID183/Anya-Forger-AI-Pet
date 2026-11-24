# 🥜 Anya Desktop Companion

> **A quiet, interactive Anya Forger (*Spy x Family*) AI companion for your Windows desktop**.

Anya lives right on top of your Windows desktop — walking along your taskbar, landing on active windows, reacting to music, splatting face-first when tossed too hard, and chatting with you powered by local or cloud LLMs and custom TTS voices.

---

## ✨ Features

* **🪟 Window-Aware Physics (60–260 FPS Lock):** Walks on your taskbar, lands on top of open windows, bounces off screen edges, and features a high-precision frame limiter.
* **💥 Grab, Throw & Face-Splat:** Pick Anya up with your mouse, toss her across the screen, watch her splat face-first on hard impacts, or hold `Shift` on release to pin her mid-air.
* **🎵 Music Player Reactivity:** Detects playback from music apps (such as Spotify) and makes Anya whistle while strolling or dance to the beat.
* **🧠 AI Brain & Telepathy:** Chat with Anya via **Ollama** or any OpenAI-compatible API, and let her peek at your active window titles to comment on what you're doing.
* **🔊 TTS Voice Synthesis:** Supports **Fish Audio** (`v1/tts`), OpenAI-compatible/local TTS servers, and built-in offline Windows speech.
* **❄️ Anya Mainframe GUI:** Obsidian-black and pastel-pink control panel with falling snow animation and live settings saving (`settings.json`).
* **👻 Hologram (Click-Through) Mode:** Pass mouse clicks right through Anya while gaming or working, and hold `Left Alt` to restore normal interaction.

---

## 🎮 Controls

| Action | Control |
| :--- | :--- |
| **Grab & Throw** | `Left-Click + Drag` |
| **Pin in Mid-Air** | Hold `Shift` while releasing `Left-Click` |
| **Quick Chat** | `Double Left-Click` on Anya |
| **Mainframe & Actions Menu** | `Right-Click` on Anya |
| **Exit Hologram Mode** | Hold `Left Alt` |

---

## 📦 Installation & Setup

### 1. Prerequisites
* **Windows 10 / 11** (x64)
* [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### 2. Run the Release Build
1. Download the release archive or `AnyaCompanion.exe` from the **Releases** page.
2. Extract all files into a folder (for example, `C:\AnyaCompanion`).
3. Launch `AnyaCompanion.exe`.

---

## ⚙️ Configuring Brain & Voice (Anya Mainframe)

Right-click Anya on your desktop and select **⚙️ Open Anya Mainframe (GUI Settings)** to configure everything without editing config files manually.

### 🧠 Brain (AI Chat & Telepathy)
Open the **BRAIN** tab:

* **Local & Free via Ollama:**
  1. Install [Ollama](https://ollama.com/) and pull a model in your terminal:
     ```bash
     ollama run gemma3:4b
     ```
  2. Set the following fields in the **BRAIN** tab:
     * **AI Provider Preset:** `Ollama (Local - Free)`
     * **API Endpoint URL:** `http://localhost:11434/v1/chat/completions`
     * **API Key:** `ollama`
     * **Model Name:** `gemma3:4b`
  3. Click **⚡ Test Brain** to verify.

* **Cloud Providers (OpenRouter / Google Gemini / OpenAI):**
  1. Get an API key from your provider (such as [OpenRouter](https://openrouter.ai/) or [Google AI Studio](https://aistudio.google.com/)).
  2. Select your preset or **Custom Endpoint**, enter your **API Endpoint URL**, **API Key**, and **Model Name** (e.g., `google/gemini-2.5-flash` or `gpt-4o-mini`).

### 🔊 Voice (TTS Output)
Open the **VOICE** tab:

* **Fish Audio:**
  1. Sign up on [Fish Audio](https://fish.audio/), create an API key (`sk-fish-...`), and copy your voice `Reference ID`.
  2. Configure in the **VOICE** tab:
     * Check **Enable Anya Voice Output (TTS)**
     * **Voice Engine / Provider:** `Fish Audio / Custom HTTP Endpoint`
     * **TTS Endpoint URL:** `https://api.fish.audio/v1/tts`
     * **TTS API Key:** `sk-fish-YOUR_API_KEY_HERE`
     * **Voice ID / Reference ID:** `YOUR_VOICE_REFERENCE_ID_HERE`
  3. Click **🔊 Test Anya's Voice**.

* **Offline / Instant:**
  * Choose **`Windows Built-in (Instant / Offline)`** for immediate speech with zero setup.

---

## 🙌 Credits
* Character: **Anya Forger** from *Spy x Family* by Tatsuya Endo.
