workspace "eugnet-patch"
    configurations { "Debug", "Release" }
    platforms { "x86", "x64" }

project "eugnet-patch"
    kind "SharedLib"
    language "C++"
    cppdialect "C++23"

    targetdir ("bin/%{cfg.platform}/%{cfg.buildcfg}")
    objdir ("obj/%{cfg.platform}/%{cfg.buildcfg}")

    systemversion "latest"

    includedirs {
        "eugnet-patch/src",
        "dep/minhook/include",  
        "dep"
    }

    libdirs {
        "dep/minhook/lib"
    }

    staticruntime "On"

    files {
        "eugnet-patch/src/**.cpp",
        "eugnet-patch/src/**.hpp",
    }

    buildoptions {
        "/std:c++latest",
        "/experimental:module"
    }

    filter "platforms:x86"
        architecture "x86"
        links { "libMinHook.x86" }

    filter "platforms:x64"
        architecture "x64"
        links { "libMinHook.x64" }

    filter "configurations:Release"
        optimize "Off"
        linkoptions { "/INCREMENTAL:NO" }

    filter {}