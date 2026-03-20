# 🔵 BlueChat

A cross-platform **peer-to-peer Bluetooth chat application** built with **.NET MAUI Blazor Hybrid** and **.NET 10**. BlueChat enables secure, real-time messaging between devices over **Bluetooth Classic** — no internet connection required — with **AES-256-GCM end-to-end encryption**.

---

## ✨ Features

- **Bluetooth Discovery & Connection** — Scan for nearby devices, host a session, or connect to a peer
- **Real-Time Messaging** — Full-duplex text messaging over Bluetooth RFCOMM
- **End-to-End Encryption** — All messages encrypted with AES-256-GCM (256-bit key, random nonce per message)
- **Cross-Platform** — Android, iOS, macOS (Catalyst), and Windows from a single codebase
- **Modern UI** — Blazor Hybrid interface with dark mode support, animations, and responsive design
- **Observability** — .NET Aspire integration with OpenTelemetry tracing, metrics, and health checks
- **AI-Ready** — Scaffolded AI API project for future Azure OpenAI and Speech Services integration

---

## 🏗️ Architecture

```
BlueChat.sln
├── BlueChat.Core              # Shared library — Bluetooth service, models, encryption
├── BlueChat.Mobile            # .NET MAUI Blazor Hybrid app — UI and user interaction
├── BlueChat.AppHost           # .NET Aspire orchestration host
├── BlueChat.ServiceDefaults   # Aspire shared defaults (telemetry, health, resilience)
```

### Dependency Graph

```
BlueChat.AppHost
  └── BlueChat.Mobile
        └── BlueChat.Core

BlueChat.AI.Api
  └── BlueChat.ServiceDefaults
```

---

## 🛠️ Tech Stack

| Technology | Version | Purpose |
|---|---|---|
| .NET | 10.0 | Target framework |
| .NET MAUI | 10.x | Cross-platform mobile framework |
| Blazor Hybrid | — | Web-based UI inside MAUI shell |
| InTheHand.Net.Bluetooth | 4.2.3 | Bluetooth Classic API (32feet.NET) |
| .NET Aspire | 13.1.0 | Cloud-native orchestration & observability |
| OpenTelemetry | 1.14.0 | Distributed tracing, metrics, logging |
| Bootstrap | 5.x | Base CSS framework |

### Planned AI Stack

| Technology | Version | Purpose |
|---|---|---|
| Azure.AI.OpenAI | 2.8.0-beta.1 | Chat completions & embeddings |
| Microsoft.CognitiveServices.Speech | 1.48.2 | Speech-to-text / text-to-speech |
| Microsoft.Extensions.AI | 10.3.0 | Unified .NET AI abstractions |

---

## 📱 Platform Support

| Platform | Minimum Version |
|---|---|
| Android | API 24 (Android 7.0) |
| iOS | 15.0 |
| macOS (Catalyst) | 15.0 |
| Windows | 10.0.17763.0 (1809) |

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [.NET MAUI workload](https://learn.microsoft.com/dotnet/maui/get-started/installation)
- Visual Studio 2022 17.13+ or VS Code with C# Dev Kit
- A device/emulator with Bluetooth support

### Install the MAUI workload

```bash
dotnet workload install maui
```

### Clone & Build

```bash
git clone https://github.com/sabbam-3/BlueChat.git
cd BlueChat
dotnet restore
dotnet build
```

### Run the Mobile App

```bash
# Android
dotnet run --project BlueChat.Mobile -f net10.0-android

# Windows
dotnet run --project BlueChat.Mobile -f net10.0-windows10.0.19041.0
```

### Run with Aspire (orchestrated)

```bash
dotnet run --project BlueChat.AppHost
```

---

## 📡 How Bluetooth Communication Works

BlueChat uses **Bluetooth Classic (RFCOMM)** via the standard **Serial Port Profile (SPP)** UUID:

```
00001101-0000-1000-8000-00805F9B34FB
```

### Connection Flow

```
1. Device A  →  Starts hosting (BluetoothListener)
                  ↓ waits up to 2 minutes for a connection

2. Device B  →  Scans for nearby Bluetooth devices
                  ↓ selects Device A from the list
               →  Connects via RFCOMM

3. Both      →  Obtain a NetworkStream for full-duplex communication
```

### Wire Protocol

Messages use a **length-prefixed framing** protocol with encryption:

```
┌──────────────────────┬─────────────────────────────────────┐
│ 4 bytes              │ N bytes (encrypted payload)         │
│ Big-endian int32     │                                     │
│ = payload length     │ [12B nonce][16B GCM tag][ciphertext]│
└──────────────────────┴─────────────────────────────────────┘
```

The ciphertext decrypts to a pipe-delimited message:

```
SenderName|SenderAddress|Payload|ISO8601Timestamp
```

### Message Size Limits

- Max encrypted payload: **256 KB**
- Max serialized text: **64 KB**

---

## 🔒 Security

| Feature | Detail |
|---|---|
| **AES-256-GCM** | Authenticated encryption — confidentiality + integrity + authentication |
| **Random nonce** | 12-byte cryptographically random nonce per message |
| **Auth tag verification** | 16-byte GCM tag detects tampering |
| **Message size validation** | Inbound and outbound messages capped at 256 KB |
| **Deserialization hardening** | Max serialized length, field count validation, timestamp format checks |
| **Input validation** | Guards against null/empty sender names and addresses |

---

## 📂 Project Structure

### BlueChat.Core

The shared class library containing all Bluetooth logic, models, and security:

```
BlueChat.Core/
├── Abstractions/
│   └── IBluetoothService.cs       # Service interface with events & methods
├── Constants/
│   ├── ConnectionState.cs         # None, Connecting, Connected, Disconnecting
│   ├── ErrorType.cs               # Connection, Hosting, Sending, etc.
│   ├── HostingState.cs            # None, Starting, Listening
│   └── ServiceConstants.cs        # SPP UUID
├── Models/
│   ├── BluetoothDevice.cs         # Device name, address, info
│   └── BluetoothMessage.cs        # Message with sender, payload, timestamp
├── Security/
│   ├── EncryptedStreamHelper.cs   # AES-GCM encrypt/decrypt + key generation
│   └── EncryptionKey.cs           # 256-bit key wrapper
├── Services/
│   └── BluetoothService.cs        # Full implementation of IBluetoothService
└── DependencyInjection.cs         # Service registration extension method
```

### BlueChat.Mobile

The .NET MAUI Blazor Hybrid application:

```
BlueChat.Mobile/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor       # App shell with bottom nav
│   │   └── NavMenu.razor          # Bottom tab bar (Connection + Chat)
│   └── Pages/
│       ├── ConnectionPage.razor   # Device scanning, hosting, connecting
│       ├── ChatPage.razor         # Real-time message exchange UI
│       └── NotFound.razor         # 404 page
├── Platforms/
│   ├── Android/                   # Android-specific (permissions, manifest)
│   ├── iOS/                       # iOS-specific
│   ├── MacCatalyst/               # macOS-specific
│   └── Windows/                   # Windows-specific
├── wwwroot/
│   ├── app.css                    # Design system, dark mode, animations
│   └── index.html                 # Blazor WebView entry point
├── MauiProgram.cs                 # App bootstrapping & DI setup
└── MainPage.xaml                  # BlazorWebView host
```

---

## 🎨 UI Overview

### Connection Page
- Scan for nearby Bluetooth devices
- Start hosting a session for others to connect
- View discovered devices and tap to connect
- Real-time status indicators (scanning, hosting, connected)
- Error display with contextual messages

### Chat Page
- Send and receive messages in real-time
- Message bubbles (sent vs. received) with timestamps
- Auto-scroll to latest message
- Send via Enter key
- Empty state when no messages exist
- Auto-navigate back to connection page on disconnect

### Theming
- Full **dark mode** support via `prefers-color-scheme: dark`
- CSS custom properties design system
- Smooth animations (fade-in, breathe, pulse, spin)
- Responsive layout with safe area handling

---

## 🤖 AI API (Planned)

The `BlueChat.AI.Api` project is scaffolded with dependencies for:

- **Azure OpenAI** — Smart replies, chat completions
- **Speech Services** — Voice-to-text and text-to-voice messaging
- **AI Agents** — Intelligent chatbot assistant
- **Swagger/OpenAPI** — API documentation

This project is not yet implemented and will be developed in future iterations.

---

## 📄 License

This project is currently unlicensed. Contact the repository owner for usage terms.

---

## 🙏 Acknowledgments

- [InTheHand (32feet.NET)](https://github.com/inthehand/32feet) — Cross-platform Bluetooth library
- [.NET MAUI](https://github.com/dotnet/maui) — Cross-platform app framework
- [.NET Aspire](https://github.com/dotnet/aspire) — Cloud-native stack for .NET
