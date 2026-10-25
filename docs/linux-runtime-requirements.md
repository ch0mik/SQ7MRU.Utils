# Linux runtime requirements

Desktop releases contain the application and .NET runtime in one executable inside TAR.GZ. They still use the operating system's native libraries and graphical session. No .NET SDK or separately installed .NET runtime is required to run the release executable.

## Ubuntu 24.04 dependency set

| Packages | Role |
| --- | --- |
| `libx11-6`, `libice6`, `libsm6` | X11 client and session integration |
| `libfontconfig1` | Font discovery and configuration |
| `libc6`, `libgcc-s1`, `libstdc++6` | Native C/C++ dependencies |
| `libicu74` | Globalization and text support |
| `libssl3t64`, `ca-certificates` | TLS/HTTPS and trusted certificate authorities |
| `zlib1g` | Compression |
| `tzdata` | Time-zone data |
| `libgssapi-krb5-2` | GSSAPI dependency included in Microsoft's documented .NET dependency set |

This is the documented Avalonia/.NET dependency set for Ubuntu 24.04, not a universal minimal dependency list for every Linux release. Most packages are already installed on a desktop system.

```sh
sudo apt update
sudo apt install -y \
  libx11-6 libice6 libsm6 libfontconfig1 \
  libc6 libgcc-s1 libstdc++6 libicu74 \
  libssl3t64 ca-certificates zlib1g tzdata libgssapi-krb5-2
```

Sources: [Avalonia native GUI dependencies](https://docs.avaloniaui.net/docs/deployment/docker) and [Microsoft's Ubuntu dependency lists](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install?tabs=dotnet10#dependencies).

## Graphical session and other distributions

The current application uses Avalonia's default X11 backend. Run it in an X11 session or a Wayland session providing XWayland. Merely installing client libraries does not provide a display server. Native Wayland is not explicitly enabled in this project. See [Avalonia Desktop Linux](https://docs.avaloniaui.net/docs/platform-specific-guides/linux).

For other Ubuntu releases, Debian, Fedora or ARM systems, use the corresponding distribution's package names and compatible architecture. In particular, ICU and OpenSSL package versions differ. Recheck the official dependency lists when changing the runtime or supported distribution; do not reuse the Ubuntu 24.04 command blindly.

The configured `linux-*` artifacts use glibc targets; this release matrix does not supply `linux-musl-*` builds. Testing library code on Linux CI does not establish that every desktop distribution or graphics setup has been validated.
