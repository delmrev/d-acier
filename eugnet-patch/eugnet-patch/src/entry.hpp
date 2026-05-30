#include "pch.hpp"

#include <Windows.h>
#include <cstdio>
#include <iostream>

#include "logger.hpp"
#include "config.hpp"
#include "hook.hpp"
#include "redirect.hpp"

void EntryPoint(HMODULE hModule) {
    AllocConsole();
    
    FILE* fp = nullptr;
    freopen_s(&fp, "CONOUT$", "w", stdout);

    SetConsoleTitleA("eugnet-patch by koteykaby");

    std::cout << "========================================" << std::endl;
    std::cout << "   eugnet-patch by koteykaby" << std::endl;
    std::cout << "========================================" << std::endl << std::endl;

    logger::initialize();
    logger::write("dll", "loaded");
    
    std::cout << "DLL initialized\n";

    InitConfig("eugnet-patch.ini");

    utils::hook::initialize_minhook();
    redirect::attach();

    logger::write("dll", "hooks attached");
}

void Shutdown()
{
    logger::write("dll", "shutdown");

    logger::shutdown();
}