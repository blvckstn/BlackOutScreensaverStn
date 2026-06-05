# Contract: CLI Arguments (Screensaver Protocol)

Windows passes exactly one of these argument forms when launching a `.scr` binary.

## Argument Forms

| Form | Mode | Notes |
|---|---|---|
| *(none)* | Settings | Same as `/c`; user double-clicked the file |
| `/s` | Screensaver | Full run |
| `/S` | Screensaver | Case-insensitive variant |
| `/c` | Settings | Open settings dialog |
| `/C` | Settings | Case-insensitive variant |
| `/c:12345` | Settings | HWND of parent window; ignored in v1 |
| `/p 12345` | Preview | HWND of preview area; app exits cleanly |
| `/P 12345` | Preview | Case-insensitive variant |

## Parser Rules

1. Comparison is case-insensitive.
2. `/c:HWND` — everything after the colon is the HWND; it is parsed but ignored.
3. `/p HWND` — HWND is the next whitespace-separated token.
4. Unknown argument → treated as Settings mode (safe default; no crash).
5. Multiple arguments → first recognised argument wins; rest ignored.

## Exit Codes

| Code | Meaning |
|---|---|
| `0` | Clean exit (all modes) |
| `1` | Unhandled exception (should never happen in production) |

## Example Parse Results

```
argv: []                          → Mode=Settings,  Hwnd=null
argv: ["/s"]                      → Mode=Screensaver, Hwnd=null
argv: ["/c"]                      → Mode=Settings,   Hwnd=null
argv: ["/c:0x0012AB"]             → Mode=Settings,   Hwnd=0x0012AB (ignored)
argv: ["/p", "77924"]             → Mode=Preview,    Hwnd=77924
argv: ["/p77924"]                 → Mode=Preview,    Hwnd=77924  (space optional)
```
