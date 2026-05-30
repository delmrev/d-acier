#pragma once

#include <string_view>
#include <format>

namespace logger
{
    void initialize();
    void shutdown();

    void write(
        std::string_view category,
        std::string_view message
    );

    template<typename... Args>
    void info(
        std::string_view category,
        std::format_string<Args...> fmt,
        Args&&... args
    );
}

template<typename... Args>
void logger::info(
    std::string_view category,
    std::format_string<Args...> fmt,
    Args&&... args
)
{
    write(
        category,
        std::format(
            fmt,
            std::forward<Args>(args)...
        )
    );
}