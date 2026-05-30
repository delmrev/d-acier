#include "pch.hpp"

#include <Windows.h>
#include <thread>

#include "entry.hpp"

BOOL APIENTRY DllMain(
    HMODULE hModule,
    DWORD ul_reason_for_call,
    LPVOID lpReserved
)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hModule);

        std::thread([hModule]()
        {
            EntryPoint(hModule);
        }).detach();

        break;

    case DLL_PROCESS_DETACH:
        Shutdown();
        break;
    }

    return TRUE;
}