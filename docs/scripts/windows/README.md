# Windows – lokal installation och start

Från repositoryts rot i PowerShell, välj installation:

```powershell
.\setup-windows.ps1 install
```

Utan ett skriptnamn visas en lista med alla Windows-skript. Tryck Enter för att välja `install.ps1` som standard, eller skriv till exempel `start.ps1`. Startaren visar dokumentations- och skriptkatalogen och ber om bekräftelse innan vald åtgärd körs.

För start och övriga Windows-kommandon:

```powershell
.\setup-windows.ps1 start
```

Starta utan hot reload med `-NoWatch`:

```powershell
.\setup-windows.ps1 start -NoWatch
