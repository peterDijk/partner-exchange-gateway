# PXF - Partner Exchange Format (fictional)

PXF is invented for this demo. It is not, and does not imitate, any real NATO or national standard.

## Encoding
- ACII only, max 1024 bytes
- Exactly six lines, each terminated by LF. Readers also accept CRLF.
- Fields are separated by exactly one space. No leading or trailing spaces.

## Lines
| # | Tag | Fields (v1) | Fields (v2) |
|---|-----|-------------|-------------|
| 1 | `PXF` | version `1` | version `2` |
| 2 | `H` | message id, observed-at, originator | same
| 3 | `U` | unit id, callsign | same
| 4 | `P` | latitude, longitude | latitude, longitude, altitude
| 5 | `S` | status (numeric) | status (letters), readiness
| 6 | `Z` | checksum | same |

## Fields
| Field | Format |
|-------|--------|
| message id | 32 hex digits (a GUID without dashes); writers use lowercase |
| observed-at | `yyyyMMddTHHmmssZ`, UTC, whole seconds |
| originator | 1-8 of `A-Z 0-9` |
# PXF — Partner Exchange Format (fictional)

PXF is invented for this demo. It is not, and does not imitate, any real NATO or national standard.

## Encoding
- ASCII only, max 1024 bytes.
- Exactly six lines, each terminated by LF. Readers also accept CRLF.
- Fields are separated by exactly one space. No leading or trailing spaces.

## Lines
| # | Tag | Fields (v1) | Fields (v2) |
|---|-----|-------------|-------------|
| 1 | `PXF` | version `1` | version `2` |
| 2 | `H` | message id, observed-at, originator | same |
| 3 | `U` | unit id, callsign | same |
| 4 | `P` | latitude, longitude | latitude, longitude, altitude |
| 5 | `S` | status (numeric) | status (letters), readiness |
| 6 | `Z` | checksum | same |

## Fields
| Field | Format |
|---|---|
| message id | 32 hex digits (a GUID without dashes); writers use lowercase |
| observed-at | `yyyyMMddTHHmmssZ`, UTC, whole seconds |
| originator | 1–8 of `A–Z 0–9` |
| unit id | 1–16 of `A–Z 0–9 -` |
| callsign | 1–8 of `A–Z 0–9` |
| latitude | sign, 2 digits, `.`, 5 digits; −90 to +90 (e.g. `+12.34560`) |
| longitude | sign, 3 digits, `.`, 5 digits; −180 to +180 (e.g. `-045.67890`) |
| altitude (v2) | 4 digits, metres, `0000`–`9999` |
| status v1 | `1` operational, `2` limited, `3` non-operational |
| status v2 | `OPR`, `LIM`, `NOP` |
| readiness (v2) | one letter `A` (highest) to `D` (lowest) |
| checksum | 4 uppercase hex digits: sum of all bytes of lines 1–5 including their LF, modulo 65536 |

The checksum detects corruption and truncation. It is not a security mechanism; authenticity is the 'pxf-signature' header.

## Example (v2)
    PXF 2
    H 0192f1a07c3e7a1b9d4e2f60a8b3c5d7 20261006T101500Z NLGW
    U BLUE-ALPHA-01 FALCON11
    P +12.34560 -045.67890 0150
    S OPR A
    Z 1C8C