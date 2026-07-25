# d-acier - Dedicated Server for Steel Division 2

This project is a dedicated server for Steel Division 2 that implements the Eugnet protocol. The goal is to allow multiplayer games without the official Eugen services, either on LAN or through a self-hosted community server.

[You can see which games are supported](#supported-games)

You can find more in our [Discord](https://discord.gg/jxcU74m6QU)

## What works

- Automatch
- Leaderboards
- Invite code
- Entering multiplayer menu
- Discover other player lobbies
- Private matches
- Join and start the game

## What doesn't work

- Invitations
- Adding/removing friends
- Auth on game copies that are not Steam (e.g GOG)

## Building

To build server binary, you need [.NET10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

```bash
# Build the binary
$ dotnet build
# Run the binary
$ dotnet run
```

To build client patch, you need [premake5](https://premake.github.io/) and [Visual Studio 2022](https://learn.microsoft.com/en-us/visualstudio/releases/2022/release-history#release-dates-and-build-numbers) compiler.

```bash
# Create building scripts
$ premake5 vs2022
# if you want to build x86 hook version replace x64 to Win32
# Build binary
$ msbuild ./eugnet-patch.sln /p:Platform=x64
```

## Usage

There's simple quick-start guide to run and play. Get the latest [release](https://github.com/delmrev/d-acier/releases/latest) and do the following guide:

### Server

> [!IMPORTANT]
> You need to open 8080, 443, 3478 and 21001 ports by default.

1. Configure the server in `config/config.json` file (update IP to serve and ports)

2. Run the server with `d-acier.exe`

### Client

1. Put `eugnet-patch.dll` and its .ini to your SD2 folder. You can use the following tool to inject: [winmm-proxy](https://github.com/koteykaby/winmm-proxy).

2. Update patch config with your host IP and ports

3. Run the game

4. Register (write any data)

> [!IMPORTANT]
> Please use only the registration window. Using other windows, such as the login or password recovery, won't work. For Wargame or Steel Division: Normandy 44 (Eugen Login), use the login window.

## Supported games

| Name | Architecture | Supported |
| :--- | :---: | :--- |
| Steel Division 2 | x64 | Native |
| Wargame : Red Dragon | x86 | Yes |
| Warno | x64 | Yes |
| Steel Division : Normandy 44 | x64 | Yes |
| Wargame : European Escalation | x86 | No |
| Wargame : Airland Battle | x86 | No |

Other games not included in this list have not been tested; they will most likely be added soon

## Special thanks

- [STUN Server](https://github.com/seanmcelroy/stungun)

## Legal Disclaimer

This is not affiliated, associated, authorized, endorsed by, or in any way officially connected with Eugen Systems or any of their subsidiaries or affiliates.

The names Steel Division 2, Steel Division:Normandy 44, Wargame or Warno as well as related names, marks, emblems, and images are registered trademarks of their respective owners. This project is for educational and preservation purposes only.
