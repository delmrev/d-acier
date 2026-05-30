#pragma once

#include "pch.hpp"

#include <cstdint>

namespace utils::hook
{
    void initialize_minhook();

    void hook(uintptr_t addr,void* hook_fn, void** orig_ptr, const char* friendly_name);

    void hook_by_name(HMODULE module_handle, const char* proc_name, void* hook_fn, void** orig_ptr, const char* friendly_name);

    void patch(uintptr_t address, const void* patch, size_t patchSize);

    void nop(uintptr_t address, size_t size);

    void jmp(uintptr_t address, void* destination);
}

namespace utils
{
    uintptr_t get_game_base_address();
}