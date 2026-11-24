\# 🥜 Anya Desktop Companion (AI Pet for Windows)



A lightweight, interactive \*\*Anya Forger\*\* (\*Spy x Family\*) AI desktop companion for Windows, inspired by the architecture of \[lachydotmcg/capri-desktop](https://github.com/lachydotmcg/capri-desktop).



Anya sits on top of your open application windows, reacts to system music, splats face-first onto your taskbar when thrown, and uses local LLMs (via \*\*Ollama\*\* or any OpenAI-compatible API) to "read your mind" and comment on what you're doing on your PC.



\---



\## ✨ Features



\* \*\*🪟 Window-Aware Physics (60 FPS):\*\* Anya walks on your taskbar, lands on top of active windows (Discord, Chrome, VS Code), and bounces off screen borders.

\* \*\*💥 Throw \& Face-Splat Mechanics:\*\* Grab and toss Anya across your monitor — if she hits the floor too hard, she splats face-first onto the ground (`splat.gif`). Hold `Shift` while releasing the mouse to pin her mid-air.

\* \*\*🎵 Reactive Audio Detection (WASAPI):\*\* When music or videos play on your PC, Anya automatically starts whistling and strolling along your screen (`whistle.gif`) or breaks into a dance (`dance.gif`).

\* \*\*🧠 Telepathy \& Local AI Brain:\*\* Double-click Anya (or right-click -> \*Read My Mind\*) to chat. Powered by \*\*Ollama\*\* (`gemma3:4b` by default) or any OpenAI-compatible endpoint.

\* \*\*👻 Hologram (Click-Through) Mode:\*\* Right-click Anya to enable Ghost/Hologram mode so your mouse clicks pass right through her while gaming. Hold `Left Alt` to make her solid again.



\---



\## 🎮 Controls



| Action | Control |

| :--- | :--- |

| \*\*Pick up \& Throw\*\* | `Left-Click + Drag` |

| \*\*Pin in Mid-Air (Anti-Gravity)\*\* | Hold `Shift` while releasing `Left-Click` |

| \*\*Open Quick Chat\*\* | `Double Left-Click` on Anya |

| \*\*Context Menu (Telepathy, Peanuts, Hologram)\*\* | `Right-Click` on Anya |

| \*\*Exit Hologram (Click-Through) Mode\*\* | Hold `Left Alt` |



\---



\## 🚀 Quick Start



\### 1. Prerequisites

\* \*\*Windows 10 / 11\*\*

\* \[.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

\* \*(Optional for AI Chat)\* \[Ollama](https://ollama.com/) installed locally:

&#x20; ```bash

&#x20; ollama run gemma3:4b

