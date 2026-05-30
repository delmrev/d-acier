#include "pch.hpp"

#include "hook.hpp"
#include "logger.hpp"
#include "config.hpp"
#include "redirect.hpp"

#include <winsock2.h>
#include <ws2tcpip.h>
#include <MinHook.h>

#include <cstring>
#include <string>

namespace
{
    constexpr std::string_view log_category = "redirect";

    bool read_ipv4(const sockaddr* addr, char* ip, size_t ip_len, uint16_t* port)
    {
        if (!addr || addr->sa_family != AF_INET)
            return false;

        auto* v4 = reinterpret_cast<const sockaddr_in*>(addr);
        if (!inet_ntop(AF_INET, &v4->sin_addr, ip, static_cast<DWORD>(ip_len)))
            return false;

        *port = ntohs(v4->sin_port);
        return true;
    }

    bool set_ipv4_addr(sockaddr_in& addr, const char* ip_str)
    {
        return inet_pton(AF_INET, ip_str, &addr.sin_addr) == 1;
    }
}

namespace redirect
{
    connect_t original_connect = nullptr;
    getaddrinfo_t original_getaddrinfo = nullptr;
    sendto_t original_sendto = nullptr;
}

int WINAPI hook_connect(SOCKET s, const sockaddr* name, int namelen)
{
    char ip[INET_ADDRSTRLEN]{};
    uint16_t port = 0;

    if (!read_ipv4(name, ip, sizeof(ip), &port))
    {
        logger::info(
            log_category,
            "connect(socket={}, namelen={}): non-IPv4 or invalid address, passthrough",
            static_cast<uintptr_t>(s),
            namelen
        );
        return redirect::original_connect(s, name, namelen);
    }

    if (!config.redirect_enabled)
    {
        logger::info(
            log_category,
            "connect(socket={}): {}:{} - redirect disabled, passthrough",
            static_cast<uintptr_t>(s),
            ip,
            port
        );
        return redirect::original_connect(s, name, namelen);
    }

    if (strcmp(ip, config.target_ip.c_str()) != 0)
    {
        logger::info(
            log_category,
            "connect(socket={}): {}:{} - not target_ip ({}), passthrough",
            static_cast<uintptr_t>(s),
            ip,
            port,
            config.target_ip
        );
        return redirect::original_connect(s, name, namelen);
    }

    sockaddr_in new_addr = *reinterpret_cast<const sockaddr_in*>(name);
    if (!set_ipv4_addr(new_addr, config.server_ip.c_str()))
    {
        logger::info(log_category, "connect(socket={}): invalid server_ip ({}), passthrough",
            static_cast<uintptr_t>(s), config.server_ip);
        return redirect::original_connect(s, name, namelen);
    }

    const uint16_t orig_port = port;
    if (config.port_map.contains(port))
        new_addr.sin_port = htons(config.port_map[port]);

    const uint16_t new_port = ntohs(new_addr.sin_port);

    logger::info(
        log_category,
        "connect(socket={}): REDIRECT {}:{} -> {}:{}{}",
        static_cast<uintptr_t>(s),
        ip,
        orig_port,
        config.server_ip,
        new_port,
        (orig_port != new_port) ? std::format(" (port_map {} -> {})", orig_port, new_port) : ""
    );

    return redirect::original_connect(s, reinterpret_cast<sockaddr*>(&new_addr), namelen);
}

int WSAAPI hook_getaddrinfo(PCSTR node, PCSTR service, const ADDRINFOA* hints, PADDRINFOA* result)
{
    static thread_local char port_buf[16];

    const char* orig_node = node ? node : "(null)";
    const char* orig_service = service ? service : "(null)";

    if (!node || !config.redirect_enabled || strcmp(node, config.target_host.c_str()) != 0)
    {
        logger::info(
            log_category,
            "getaddrinfo(node={}, service={}): passthrough{}",
            orig_node,
            orig_service,
            !config.redirect_enabled ? " (redirect disabled)" :
            !node ? " (null node)" :
            std::format(" (node != target_host {})", config.target_host)
        );
        return redirect::original_getaddrinfo(node, service, hints, result);
    }

    const char* new_node = config.server_ip.c_str();
    const char* new_service = service;

    if (service)
    {
        try
        {
            const int port = std::stoi(service);

            if (config.port_map.contains(port))
            {
                sprintf_s(port_buf, "%d", config.port_map[port]);
                new_service = port_buf;

                logger::info(
                    log_category,
                    "getaddrinfo: REDIRECT node {} -> {}, service {} -> {} (port_map {} -> {})",
                    orig_node,
                    new_node,
                    orig_service,
                    new_service,
                    port,
                    config.port_map[port]
                );
            }
            else
            {
                logger::info(
                    log_category,
                    "getaddrinfo: REDIRECT node {} -> {}, service {} (no port_map for {})",
                    orig_node,
                    new_node,
                    orig_service,
                    port
                );
            }
        }
        catch (...)
        {
            logger::info(
                log_category,
                "getaddrinfo: REDIRECT node {} -> {}, service {} (non-numeric, unchanged)",
                orig_node,
                new_node,
                orig_service
            );
        }
    }
    else
    {
        logger::info(
            log_category,
            "getaddrinfo: REDIRECT node {} -> {}, service=null",
            orig_node,
            new_node
        );
    }

    return redirect::original_getaddrinfo(new_node, new_service, hints, result);
}

int WSAAPI hook_sendto(SOCKET s, const char* buf, int len, int flags, const sockaddr* to, int tolen)
{
    char ip[INET_ADDRSTRLEN]{};
    uint16_t port = 0;

    if (!read_ipv4(to, ip, sizeof(ip), &port))
        return redirect::original_sendto(s, buf, len, flags, to, tolen);

    if (!config.redirect_enabled || strcmp(ip, config.target_ip.c_str()) != 0)
        return redirect::original_sendto(s, buf, len, flags, to, tolen);

    sockaddr_in new_addr = *reinterpret_cast<const sockaddr_in*>(to);
    if (!set_ipv4_addr(new_addr, config.server_ip.c_str()))
        return redirect::original_sendto(s, buf, len, flags, to, tolen);

    const uint16_t orig_port = port;
    if (config.port_map.contains(port))
        new_addr.sin_port = htons(config.port_map[port]);

    const uint16_t new_port = ntohs(new_addr.sin_port);

    logger::info(
        log_category,
        "sendto(socket={}, len={}, flags={}): REDIRECT {}:{} -> {}:{}{}",
        static_cast<uintptr_t>(s),
        len,
        flags,
        ip,
        orig_port,
        config.server_ip,
        new_port,
        (orig_port != new_port) ? std::format(" (port_map {} -> {})", orig_port, new_port) : ""
    );

    return redirect::original_sendto(s, buf, len, flags, reinterpret_cast<sockaddr*>(&new_addr), tolen);
}

void redirect::attach()
{
    HMODULE ws2 = GetModuleHandleA("ws2_32.dll");
    if (!ws2)
    {
        ws2 = LoadLibraryA("ws2_32.dll");
        logger::info(
            log_category,
            "ws2_32.dll was not loaded; LoadLibrary -> 0x{:x}",
            reinterpret_cast<uintptr_t>(ws2)
        );
    }
    else
    {
        logger::info(
            log_category,
            "ws2_32.dll at 0x{:x}",
            reinterpret_cast<uintptr_t>(ws2)
        );
    }

    logger::info(
        log_category,
        "config: enabled={} target_ip={} target_host={} server_ip={} port_map_entries={}",
        config.redirect_enabled,
        config.target_ip,
        config.target_host,
        config.server_ip,
        config.port_map.size()
    );

    for (const auto& [from, to] : config.port_map)
        logger::info(log_category, "  port_map: {} -> {}", from, to);

    utils::hook::hook_by_name(ws2, "connect", (void*)hook_connect, (void**)&original_connect, "connect");
    utils::hook::hook_by_name(ws2, "getaddrinfo", (void*)hook_getaddrinfo, (void**)&original_getaddrinfo, "getaddrinfo");
    utils::hook::hook_by_name(ws2, "sendto", (void*)hook_sendto, (void**)&original_sendto, "sendto");

    logger::info(
        log_category,
        "hooks ready: connect={} getaddrinfo={} sendto={}",
        original_connect != nullptr,
        original_getaddrinfo != nullptr,
        original_sendto != nullptr
    );
}
