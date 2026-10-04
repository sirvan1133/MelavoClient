# Melavo VPN 0.8.8 — UI review

## Fixed
- Missing search placeholder now renders in both the native editor and review captures, including empty focused inputs.
- Toolbar captions fit; compact windows use two rows.
- Compact layout retains a usable scrolling server table and readable subscription actions.
- Empty, selected, busy and connected states consistently control available actions, including after language changes.
- Subscription edit save button no longer expands into the remaining dialog space.
- Shared configuration text wraps; scrollbar counts rendered lines.
- Shorter traffic caption fits narrow metric cells.

## Verification
- Release Customer build: zero compiler warnings/errors.
- UI checks: navigation, rapid page switching, both palettes, Classic/Glass changes, English captions, RTL, filtering, favorites, scroll preservation, menu animation, traffic repaint stability, compact layout, empty action state.
- Render review: Home/Settings/About and add subscription/edit subscription/add config/edit config/share dialogs in English/Persian and light/dark, plus blank Customer, no-match, minimum 1240×780 and Classic scrolling.
- Management checks on an isolated profile: add/rename/change URL/failure preservation, single configs, validated edits, removal, encrypted persistence; no customer subscriptions fetched.
- Real ICMP loopback checks for supported endpoint forms passed.
- Hardening checks: hash rejection before launch, session cleanup, update/rollback cleanup, VMess transport retention, actual loopback SOCKS byte counters.
- Customer profile roundtrip reports zero saved groups.

## Limits
These are automated behavior checks and visual inspection of rendered controls. They do not certify every possible network/provider or real display scaling at 125%/150%/200%. Native non-client dialog chrome is not faithfully represented by DrawToBitmap. The tests did not disconnect or modify the user's installed VPN.

The distribution contains no customer data, subscription URL or imported configuration. Documentation/release introduction remains general.
