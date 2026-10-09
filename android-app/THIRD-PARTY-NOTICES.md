# Third-party components
- AndroidLibXrayLite v26.9.30: LGPL-3.0. Source: https://github.com/2dust/AndroidLibXrayLite/tree/v26.9.30 ; license: LICENSE-Core.txt. Rebuild instructions are in upstream README.
- Xray-core b26a91de4f32 (v26.9.30 core dependency): MPL-2.0. Source: https://github.com/XTLS/Xray-core/tree/b26a91de4f32 . License: https://github.com/XTLS/Xray-core/blob/b26a91de4f32/LICENSE .
- Go runtime and gomobile: BSD-3-Clause; upstream sources: https://go.dev/ and https://github.com/golang/mobile .
- ZXing core 3.5.3: Apache-2.0; source: https://github.com/zxing/zxing/tree/zxing-3.5.3 .
- Babel helpers: MIT; source: https://github.com/babel/babel .
The native AAR is pinned by SHA-256 and linked as a separate shared library. The app source can be rebuilt and signed with your own key; when replacing the native core, update the expected hash in app/build.gradle to match your rebuilt library.
