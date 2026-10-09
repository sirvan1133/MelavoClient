# Android verification — 2026-10-09

- Release APKs: ARMv7, ARM64, x86, x86_64, universal; minimum API 24, target API 36.
- All APK signatures checked. Each uses the same publisher certificate.
- Official AndroidLibXrayLite v26.9.30 pinned by SHA-256 at build time.
- Native entries use 16 KiB ZIP alignment. 64-bit ELF loads align to 16 KiB; 32-bit builds align to 4 KiB.
- API 24 and API 37 x86_64 emulators: native core, SOCKS transfer, traffic counters, ICMP, encrypted storage, complete configuration sharing, English/Persian interface.
- Real VPN/TUN test on both emulators: traffic from a separate probe application's UID reached a private test server through VLESS. Disconnect closed TUN.
- x86 package also exercised on API 24. ARM package contents inspected; physical ARM hardware was unavailable.
- Signed universal APK installed and launched on API 24.
- Legacy Android WebViews receive local JavaScript compatibility code and a simplified glass renderer.
- Lint and release build gates passed. The VPN's foreground-service exemption is documented in the manifest.
- Customer APKs contain no saved subscriptions or configurations. Test packages and signing credentials are excluded from distribution.
