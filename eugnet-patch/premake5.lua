workspace "eugnet-patch"
    configurations { "Debug", "Release" }
    architecture "x64"

project "eugnet-patch"
    kind "SharedLib"
    language "C++"
    cppdialect "C++23"

    targetdir "bin/%{cfg.buildcfg}"
    objdir "obj/%{cfg.buildcfg}"

    systemversion "latest"

    includedirs {
        "eugnet-patch/src",
        "dep/minhook/include",  
        "dep"
    }

    libdirs {
        "dep/minhook/lib"
    }

    links {
        "libMinHook.x64"
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

    filter "configurations:Release"
        optimize "Off"
        linkoptions { "/INCREMENTAL:NO" }

    filter {}