# Simple patch for Eugen Systems games to redirect traffic

This patch redirects traffic from official servers to custom ones.

## Building

You need VS2022 compiler and premake5 to build.

Use those commands:

```bash
# Generate build scripts with premake5
$ premake5 vs2022
# if you want to build x86 hook version replace x64 to Win32
# Build binary
$ msbuild ./eugnet-patch.sln /p:Platform=x64
```
