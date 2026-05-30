#include "logger.hpp"

#include <windows.h>

#include <fstream>
#include <iostream>
#include <mutex>
#include <string>

namespace logger
{
    static std::ofstream g_file;
    static std::mutex g_mutex;

    static std::string current_time()
    {
        SYSTEMTIME st{};
        GetLocalTime(&st);

        return std::format(
            "{:04}-{:02}-{:02} {:02}:{:02}:{:02}",
            st.wYear,
            st.wMonth,
            st.wDay,
            st.wHour,
            st.wMinute,
            st.wSecond
        );
    }

    void initialize()
    {
        g_file.open(
            "eugnet-patch.log",
            std::ios::out | std::ios::trunc
        );
    }

    void shutdown()
    {
        if (g_file.is_open())
            g_file.close();
    }

    void write(
        std::string_view category,
        std::string_view message
    )
    {
        std::lock_guard lock(g_mutex);

        const std::string line = std::format(
            "[{}] {} - {}\n",
            current_time(),
            category,
            message
        );

        if (g_file.is_open())
        {
            g_file << line;
            g_file.flush();
        }

        std::cout << line;
        std::cout.flush();
    }
}