# Windows – lokal installation och start

Installera från repositoryts rot i PowerShell:

```powershell
.\local-app-install.ps1
```

För start och övriga Windows-kommandon:

```powershell
Set-Location .\scripts\windows
.\start.ps1
```

Starta utan hot reload med `-NoWatch`:

```powershell
.\start.ps1 -NoWatch
```

Den äldre root-sökvägen `local-app-install.ps1` finns kvar som kompatibilitetsstartare.