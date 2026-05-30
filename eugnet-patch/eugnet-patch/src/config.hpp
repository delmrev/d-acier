#pragma once

#include <string>
#include <unordered_map>

struct Config
{
    bool redirect_enabled = true;

    std::string target_ip;
    std::string target_host;

    std::string server_ip;

    std::unordered_map<int, int> port_map;
};

// global config
extern Config config;

void InitConfig(const std::string& path);