# Melavo VPN for Android
Android 7+ / ARMv7, ARM64, x86, x86_64. The UI is bundled locally and uses the same Melavo design.
The VPN uses AndroidLibXrayLite v26.9.30 (LGPL-3.0), pinned SHA-256 in app/build.gradle. Upstream source: https://github.com/2dust/AndroidLibXrayLite/tree/v26.9.30
Build with JDK 21 and Gradle 9.1. Set ANDROID_HOME or local.properties. Release signing: MELAVO_KEYSTORE and MELAVO_SIGNING_PASSWORD environment variables; alias melavo. Never commit your keystore/password.

Run tools/fetch-core.ps1 to download the pinned Android AAR before building. The checked-in ui.html is already transpiled for Chrome 51+; editing ui.source.html requires npm install and npm run build:ui.
