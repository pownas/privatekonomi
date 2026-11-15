# Visual Guide: Bank Dropdown Feature

## Screenshots Description

This guide describes the expected UI appearance; the application currently targets .NET 11.

## 1. Account Settings Page (/settings/accounts)

### Before Changes
- "Bank/Institution" field was a plain text input
- Users could type any text
- No visual indication of bank brands

### After Changes
- "Bank/Institution" is now a dropdown selector
- Shows 6 predefined banks with color indicators
- Professional, consistent look

## 2. Edit Account Dialog - Dropdown Closed

```
The dialog will show:
┌──────────────────────────────────────┐
│ Bank/Institution                    ▼│
│ ┌──────────────────────────────────┐ │
│ │ Swedbank                         │ │ <- Selected bank with orange square
│ └──────────────────────────────────┘ │
└──────────────────────────────────────┘
```

Visual elements:
- Small orange square (16x16px) next to "Swedbank"
- Dropdown arrow on the right
- Clear (X) button appears on hover

## 3. Edit Account Dialog - Dropdown Open

When user clicks the dropdown, they see:

```
┌──────────────────────────────────────┐
│ Bank/Institution                    ▲│
│ ┌──────────────────────────────────┐ │
│ │                                  │ │
│ └──────────────────────────────────┘ │
│ ╔══════════════════════════════════╗ │
│ ║ ⬛ Handelsbanken                  ║ │ <- Dark blue square
│ ║ 🟥 ICA-banken                    ║ │ <- Red square
│ ║ 🟦 Nordea                        ║ │ <- Blue square
│ ║ 🟩 SEB                           ║ │ <- Green square
│ ║ 🟧 Swedbank                      ║ │ <- Orange square (highlighted)
│ ║ 🟩 Avanza                        ║ │ <- Turquoise square
│ ╚══════════════════════════════════╝ │
└──────────────────────────────────────┘
```

Each bank option shows:
- 16x16px colored square matching bank's brand color
- 8px spacing between square and bank name
- Hover effect highlights the entire row
- Selected bank has a checkmark or different background

## 4. Color Auto-Fill Effect

### Before selecting bank:
```
┌──────────────────────────────────────┐
│ Färg (hex)                           │
│ ┌──────────────────────────────────┐ │
│ │ #1976D2                          │ │ <- Default blue
│ └──────────────────────────────────┘ │
└──────────────────────────────────────┘
```

### After selecting Swedbank:
```
┌──────────────────────────────────────┐
│ Färg (hex)                           │
│ ┌──────────────────────────────────┐ │
│ │ #FF7900                          │ │ <- Auto-filled with Swedbank orange!
│ └──────────────────────────────────┘ │
└──────────────────────────────────────┘
```

The color field automatically updates with the bank's official color.

## 5. Full Dialog View

The complete "Redigera konto" (Edit Account) dialog will look like:

```
╔════════════════════════════════════════════════════╗
║  Redigera konto                                  ×║
╠════════════════════════════════════════════════════╣
║                                                    ║
║  Kontonamn*                                        ║
║  ┌──────────────────────────────────────────────┐ ║
║  │ Mitt sparkonto                               │ ║
║  └──────────────────────────────────────────────┘ ║
║                                                    ║
║  Kontotyp*              Bank/Institution        ▼ ║
║  ┌────────────────────┐ ┌─────────────────────────┐║
║  │ 💰 Sparkonto       │ │ 🟧 Swedbank             │║
║  └────────────────────┘ └─────────────────────────┘║
║                                                    ║
║  Clearingnummer        Kontonummer                 ║
║  ┌────────────────────┐ ┌─────────────────────────┐║
║  │ 8327               │ │ 1234567                 │║
║  └────────────────────┘ └─────────────────────────┘║
║  För svenska bankkonton                            ║
║                                                    ║
║  Valuta*               Kontoplan (BAS)             ║
║  ┌────────────────────┐ ┌─────────────────────────┐║
║  │ SEK                │ │ 1940                    │║
║  └────────────────────┘ └─────────────────────────┘║
║                        Koppla till BAS-kontoplan   ║
║                                                    ║
║  Färg (hex)                                        ║
║  ┌──────────────────────────────────────────────┐ ║
║  │ #FF7900                                      │ ║
║  └──────────────────────────────────────────────┘ ║
║  Färgkod i hex-format                              ║
║                                                    ║
║  Aktuellt saldo: 25 000,00 kr                      ║
║  (beräknat från ingående saldo och transaktioner)  ║
║                                                    ║
╠════════════════════════════════════════════════════╣
║                           [ AVBRYT ]  [ SPARA ]    ║
╚════════════════════════════════════════════════════╝
```

## 6. Mobile View

On mobile devices (screens < 960px):

- Fields stack vertically
- "Kontotyp" on one row
- "Bank/Institution" on next row (full width)
- Dropdown expands to full width
- Color squares remain 16x16px
- Easy to tap on mobile

## 7. Color Palette Visualization

Here's what the bank colors look like:

```
Handelsbanken: ████ #003781 (Dark Navy Blue)
ICA-banken:    ████ #E3000F (Bright Red)
Nordea:        ████ #0000A0 (Royal Blue)
SEB:           ████ #60CD18 (Bright Green)
Swedbank:      ████ #FF7900 (Orange)
Avanza:        ████ #00C281 (Turquoise Green)
```

## 8. User Flow Example

**Scenario: User adding a new Handelsbanken account**

1. User clicks "Lägg till konto" button
2. Dialog opens with empty form
3. User enters "Mitt lönekonto" as account name
4. User selects "Lönekonto" as account type
5. User clicks "Bank/Institution" dropdown
6. Dropdown shows all 6 banks with colored squares
7. User clicks "⬛ Handelsbanken" (dark blue square)
8. Dropdown closes, showing "⬛ Handelsbanken"
9. **Färg (hex) field auto-fills with "#003781"**
10. User fills in remaining details
11. User clicks "Lägg till"
12. Account is saved with Handelsbanken and dark blue color

## 9. Accessibility Features

The implementation includes:

- **aria-label**: "Välj bank eller institution"
- **Keyboard navigation**: Arrow keys to navigate banks
- **Search**: Start typing to filter banks
- **Screen reader**: Announces bank names and colors
- **Focus indicators**: Clear visual focus on selected item
- **Clear action**: Easy to remove selection

## 10. Expected Behavior

✅ **Works:**
- Selecting a bank updates the institution field
- Color field auto-fills with bank's color
- Dropdown is searchable (type "SEB" to jump to it)
- Clearing dropdown clears institution field
- Color can still be manually changed after auto-fill
- Existing accounts with custom banks still work

❌ **Doesn't work (by design):**
- Cannot type custom bank names
- Must use dropdown to select from predefined banks
- (Users with non-standard banks keep their existing values)

## Notes for Testing

When manually testing the feature, verify:

1. ✅ All 6 banks appear in dropdown
2. ✅ Each bank has correct color square
3. ✅ Selecting bank auto-fills color field
4. ✅ Dropdown is clearable
5. ✅ Search/filter works in dropdown
6. ✅ Mobile responsive (fields stack vertically)
7. ✅ Existing accounts load correctly
8. ✅ Saving account works with dropdown selection
9. ✅ Color persists after save
10. ✅ No console errors

## Expected Screenshot Locations

When the application runs, take screenshots of:

1. **accounts-page-overview.png** - The /settings/accounts page
2. **edit-dialog-dropdown-closed.png** - Dialog with bank selected
3. **edit-dialog-dropdown-open.png** - Dropdown showing all banks
4. **add-dialog-bank-selection.png** - Adding new account with bank selection
5. **color-auto-fill.png** - Before/after selecting bank showing color change
6. **mobile-view.png** - Dialog on mobile device
7. **handelsbanken-account.png** - Complete account with Handelsbanken selected
8. **accounts-with-colors.png** - Account list showing different bank colors

These screenshots should be added to the PR once the application is running.
