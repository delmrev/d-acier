# d-acier - Dedicated Server for Steel Division 2

This project is a dedicated server for Steel Division 2 that implements the Eugnet protocol. The goal is to allow multiplayer games without the official Eugen services, either on LAN or through a self-hosted community server.

For now, it supports only SD2, but the protocol code should be similar for other games because they use the same engine.

You can find more in our [Discord](https://discord.gg/UM4CjfqAC)

## What works

- Entering multiplayer menu
- Adding/removing friends
- Discover other player lobbies
- Join and start the game

## What doesn't work

- Automatch
- Leaderboards
- Invitations
- Private matches
- Auth on game copies that are not Steam (e.g GOG)

## Building

To build server binary, you need [.NET10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

```bash
# Build the binary
$ dotnet build
# Run the binary
$ dotnet run
```

To build client patch, you need [premake5](https://premake.github.io/) and [Visual Studio 2022](https://learn.microsoft.com/ru-ru/visualstudio/releases/2022/release-history#release-dates-and-build-numbers) compiler.

```bash
# Create building scripts
$ premake5 vs2022
# Build binary
$ msbuild ./eugnet-patch.sln
```

## Usage

There's simple quick-start guide to run and play. Get the latest [release](https://github.com/delmrev/d-acier/releases/latest) and do the following guide:

### Server

> [!IMPORTANT]
> You need to open 8080, 443, 3478 and 21001 ports by default.

1. Generate SSL certificate via `PfxGenerator.fsx` script in the Tools folder. Rename your certificate to server.pfx and put it into `cert` folder.

2. Configure the server in `config/config.json` file (update IP to serve and ports)

3. Run the server with d-acier.exe

### Client

1. Put `eugnet-patch.dll` and its .ini to your SD2 folder. You can use the following tool to inject: [winmm-proxy](https://github.com/koteykaby/winmm-proxy).

2. Update patch config with your host IP and ports

3. Run the game

## Special thanks

- [STUN Server](https://github.com/seanmcelroy/stungun)
