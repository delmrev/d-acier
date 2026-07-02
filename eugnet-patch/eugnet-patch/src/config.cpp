#include "pch.hpp"

#include "config.hpp"

#include "mini/ini.h"

#include <filesystem>
#include <string>
#include <cstdlib>

Config config;

static void FatalError(const std::string& message)
{
    MessageBoxA(
        nullptr,
        message.c_str(),
        "Fatal error",
        MB_ICONERROR | MB_OK
    );

    std::exit(1);
}

static std::string strip_quotes(std::string value)
{
    while (!value.empty() && (value.front() == ' ' || value.front() == '\t'))
        value.erase(value.begin());

    while (!value.empty() && (value.back() == ' ' || value.back() == '\t' || value.back() == '\r'))
        value.pop_back();

    if (value.size() >= 2)
    {
        const char q = value.front();

        if ((q == '"' || q == '\'') && value.back() == q)
            return value.substr(1, value.size() - 2);
    }

    return value;
}

static int to_int(const std::string& value)
{
    try
    {
        const std::string stripped = strip_quotes(value);
        return stripped.empty() ? 0 : std::stoi(stripped);
    }
    catch (...)
    {
        return 0;
    }
}

static void ValidateString(const std::string& value, const char* name)
{
    if (value.empty())
    {
        FatalError(
            std::string("Missing configuration value:\n") + name
        );
    }
}

static void ValidatePort(int port, const char* name)
{
    if (port <= 0 || port > 65535)
    {
        FatalError(
            std::string("Invalid or missing port:\n") + name
        );
    }
}

void InitConfig(const std::string& path)
{
    if (!std::filesystem::exists(path))
    {
        FatalError(
            "Config file not found:\n" + path
        );
    }

    mINI::INIFile file(path);
    mINI::INIStructure ini;

    if (!file.read(ini))
    {
        FatalError(
            "Failed to read config file:\n" + path
        );
    }

    auto& server = ini["server"];
    auto& settings = ini["settings"];

    config.server_ip   = strip_quotes(server.get("ip"));
    config.target_ip   = strip_quotes(settings.get("original-ip"));
    config.target_host = strip_quotes(settings.get("original-addr"));

    ValidateString(config.server_ip, "server.ip");
    ValidateString(config.target_ip, "settings.original-ip");
    ValidateString(config.target_host, "settings.original-addr");

    config.port_map[21000] = to_int(server.get("eugnet-tcp"));
    config.port_map[21001] = to_int(server.get("eugnet-tcp-alt"));
    config.port_map[80]    = to_int(server.get("eugnet-http"));
    config.port_map[8080]  = to_int(server.get("eugnet-http-alt"));
    config.port_map[443]   = to_int(server.get("eugnet-http-secure"));
    config.port_map[3478]  = to_int(server.get("eugnet-stun"));
    config.port_map[10000] = to_int(server.get("old-eugnet-tcp"));
    config.port_map[10001] = to_int(server.get("old-eugnet-tcp-alt"));

    ValidatePort(config.port_map[21000], "server.eugnet-tcp");
    ValidatePort(config.port_map[21001], "server.eugnet-tcp-alt");
    ValidatePort(config.port_map[80],    "server.eugnet-http");
    ValidatePort(config.port_map[8080],  "server.eugnet-http-alt");
    ValidatePort(config.port_map[443],   "server.eugnet-http-secure");
    ValidatePort(config.port_map[3478],  "server.eugnet-stun");
    ValidatePort(config.port_map[10000],   "server.old-eugnet-tcp");
    ValidatePort(config.port_map[10001],  "server.old-eugnet-tcp-alt");

    config.redirect_enabled = true;
}