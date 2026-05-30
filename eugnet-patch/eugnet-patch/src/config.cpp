#include "config.hpp"

#include "mini/ini.h"
#include <string>

Config config;

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

static int to_int(const std::string& v)
{
    try
    {
        const std::string stripped = strip_quotes(v);
        return stripped.empty() ? 0 : std::stoi(stripped);
    }
    catch (...)
    {
        return 0;
    }
}

void InitConfig(const std::string& path)
{
    mINI::INIFile file(path);
    mINI::INIStructure ini;

    if (!file.read(ini))
        return;

    auto& server = ini["server"];
    auto& settings = ini["settings"];

    config.server_ip   = strip_quotes(server.get("ip"));

    config.target_ip   = strip_quotes(settings.get("original-ip"));
    config.target_host = strip_quotes(settings.get("original-addr"));

    config.redirect_enabled = true;

    config.port_map[21000] = to_int(server.get("eugnet-tcp"));
    config.port_map[21001] = to_int(server.get("eugnet-tcp-alt"));
    config.port_map[80]    = to_int(server.get("eugnet-http"));
    config.port_map[8080]  = to_int(server.get("eugnet-http-alt"));
    config.port_map[443]   = to_int(server.get("eugnet-http-secure"));
    config.port_map[3478]  = to_int(server.get("eugnet-stun"));
}