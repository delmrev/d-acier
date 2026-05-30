#include "hook.hpp"
#include "logger.hpp"

#include "config.hpp"

#include <vector>
#include <cstring>
#include <MinHook.h>

#define LOG_INFO(msg) logger::write("hook", msg)
#define LOG_ERROR(msg) logger::write("error", msg)

void utils::hook::initialize_minhook()
{
    if (MH_Initialize() != MH_OK)
    {
        LOG_ERROR("Failed to initialize MinHook");
        return;
    }

    LOG_INFO("MinHook initialized");
}

void utils::hook::hook(uintptr_t addr, void* hook_fn, void** orig_ptr, const char* friendly_name)
{
    if (!addr)
    {
        LOG_ERROR(std::format("{} address is null (0x{:x})", friendly_name, addr));
        return;
    }

    MH_STATUS status = MH_CreateHook(reinterpret_cast<void*>(addr), hook_fn, orig_ptr);
    if (status != MH_OK)
    {
        LOG_ERROR(std::format("Failed to create hook: {} ({})", friendly_name, MH_StatusToString(status)));
        return;
    }

    status = MH_EnableHook(reinterpret_cast<void*>(addr));
    if (status != MH_OK)
    {
        LOG_ERROR(std::format("Failed to enable hook: {} ({})", friendly_name, MH_StatusToString(status)));
        return;
    }

    LOG_INFO(std::format("{} hook installed at 0x{:x}", friendly_name, addr));
}

void utils::hook::hook_by_name(HMODULE module_handle, const char* proc_name,
    void* hook_fn, void** orig_ptr, const char* friendly_name)
{
    void* target_addr = reinterpret_cast<void*>(GetProcAddress(module_handle, proc_name));

    if (!target_addr)
    {
        LOG_ERROR(std::format("Failed to find proc: {} ({})", friendly_name, proc_name));
        return;
    }

    MH_STATUS status = MH_CreateHook(target_addr, hook_fn, orig_ptr);
    if (status != MH_OK)
    {
        LOG_ERROR(std::format("Failed to create hook: {} ({})", friendly_name, MH_StatusToString(status)));
        return;
    }

    status = MH_EnableHook(target_addr);
    if (status != MH_OK)
    {
        LOG_ERROR(std::format("Failed to enable hook: {} ({})", friendly_name, MH_StatusToString(status)));
        return;
    }

    LOG_INFO(std::format("{} hook installed for {} at 0x{:x}",
        friendly_name,
        proc_name,
        reinterpret_cast<uintptr_t>(target_addr)));
}

void utils::hook::patch(uintptr_t address, const void* patch, size_t patchSize)
{
    if (!address || !patch || patchSize == 0)
        return;

    void* addr = reinterpret_cast<void*>(address);

    DWORD oldProtect;
    if (!VirtualProtect(addr, patchSize, PAGE_EXECUTE_READWRITE, &oldProtect))
    {
        LOG_ERROR(std::format("VirtualProtect failed at 0x{:x}", address));
        return;
    }

    std::memcpy(addr, patch, patchSize);

    FlushInstructionCache(GetCurrentProcess(), addr, patchSize);

    DWORD temp;
    VirtualProtect(addr, patchSize, oldProtect, &temp);

    LOG_INFO(std::format("Patched memory at 0x{:x} ({} bytes)", address, patchSize));
}

void utils::hook::nop(uintptr_t address, size_t size)
{
    std::vector<uint8_t> nops(size, 0x90);
    patch(address, nops.data(), size);
}

void utils::hook::jmp(uintptr_t address, void* destination)
{
    uint8_t jmpInstruction[5]{};

    jmpInstruction[0] = 0xE9;

    uintptr_t rel = reinterpret_cast<uintptr_t>(destination)
        - (address + 5);

    *reinterpret_cast<uint32_t*>(&jmpInstruction[1]) = static_cast<uint32_t>(rel);

    patch(address, jmpInstruction, sizeof(jmpInstruction));
}

uintptr_t utils::get_game_base_address()
{
    HMODULE main_module =
        GetModuleHandleA(NULL);

    uintptr_t base = reinterpret_cast<uintptr_t>(main_module);

    LOG_INFO(std::format("Game base address: 0x{:x}", base));

    return base;
}