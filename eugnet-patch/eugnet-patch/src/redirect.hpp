#pragma once

#include "pch.hpp"

namespace redirect
{
    using connect_t = int (WINAPI*)(SOCKET, const sockaddr*, int);
    using getaddrinfo_t = int (WSAAPI*)(PCSTR, PCSTR, const ADDRINFOA*, PADDRINFOA*);
    using sendto_t = int (WSAAPI*)(SOCKET, const char*, int, int, const sockaddr*, int);

    void attach();

    extern connect_t original_connect;
    extern getaddrinfo_t original_getaddrinfo;
    extern sendto_t original_sendto;
}