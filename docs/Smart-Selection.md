# Smart selection 0.10.1

Four stages: HTTP tunnel filter (10 concurrent, 2500 ms per HTTP probe), stability (top five, three more samples at 100 ms intervals), bounded 100 KB throughput (top two), and historical scoring/selection.

Metrics: first real TLS handshake to the HTTPS test endpoint through SOCKS; four HTTP 204 latencies; failed-sample ratio; nearest-rank p90; mean absolute difference between consecutive successful samples. TCP connect timing is diagnostic only for an HTTP timeout with TCP below 80 ms. It never makes a failed tunnel eligible. TLS time is measured separately from HTTP RTT; this does not expose the private Xray transport's QUIC handshake.

Raw cost follows the supplied equation exactly. Failure count decays with a ten-minute half-life; EWMA uses alpha 0.35. Quality = clamp(round(100 - EWMA/15), 0, 100). History stores SHA256 profile keys, EWMA and failure/cooldown times in portable data/smart-history.json; credentials are not stored in this file.

Throughput is KB/s (bytes / 1024 / seconds), despite the requested variable name throughput_kbps. Only two candidates download at most 102400 application payload bytes each. Total funnel also consumes HTTP, TLS and tunnel overhead, so 200 KB is not a total network-bandwidth cap. Probes use SOCKS with no direct fallback, validate TLS normally and require HTTP 204. The download is bounded even if a server ignores Range.

THROTTLED (<150 KB/s) candidates are placed behind candidates with verified healthy bandwidth. This is a deliberately heavy selection penalty implemented as a ranking tier; it does not silently change the exact requested raw-cost/EWMA/quality equations. Scores for candidates without throughput are not fabricated. Duplicated profiles are tested once.

Passive checks run every 15 seconds or after three TX-active seconds with unchanged RX. Two consecutive failures record a decaying failure, set a two-minute cooldown and try the next eligible finalist. If necessary a new funnel runs, excluding cooldown entries. Periodic reevaluation is every five minutes; healthy routes switch only when the challenger score is strictly below 75% of the current score. Expected/manual disconnects do not count as failures. Unexpected core exits trigger smart recovery.

Hot-swap uses the Xray HandlerService and RoutingService APIs: validate a candidate in a separate tunnel, add its outbound, replace the route for the existing SOCKS inbound, verify HTTP 204 and roll back on failure. Main Xray PID, local SOCKS port and TUN remain running. Existing TCP streams cannot be migrated across remote transports: old outbound handlers remain until disconnect to let their existing streams drain. This is live routing failover for new flows, not a promise of zero packet loss or uninterrupted calls/downloads. If the engine itself exits, it must be restarted.

Core preparation/startup has its own bounded deadline, outside the 2500 ms HTTP deadline. The complete funnel has a five-minute safety deadline. Passive healthy checks and background score progress do not produce routine notifications.

Official API references:
- https://github.com/XTLS/Xray-core/blob/main/main/commands/all/api/outbounds_add.go
- https://github.com/XTLS/Xray-core/blob/main/main/commands/all/api/rules_add.go

Verification: deterministic funnel tests verify 10-way concurrency, 1/4 sample counts, exactly two throughput candidates, fake-ping elimination, exact math, EWMA, history, loss/jitter/p90, cooldown, 25% hysteresis, two-failure circuit, three-second stall, cancellation and quality mapping. A real Xray fixture verifies live route changes and rollback on the same PID and SOCKS port using local HTTP. Portrait UI checks cover English/Persian, small viewports, real host list rendering, 100-row scrolling and no JavaScript errors. Customer credentials and real customer VPN endpoints were not used in these tests.
