# Windows – lokal installation och start

Installera från repositoryts rot i PowerShell:

```powershell
.\setup-windows.ps1
```

Startaren visar dokumentationen och implementationsskriptet `scripts\windows\install.ps1` och ber om godkännande innan installationen startar.

För start och övriga Windows-kommandon:

```powershell
Set-Location .\scripts\windows
.\start.ps1
```

Starta utan hot reload med `-NoWatch`:

```powershell
.\start.ps1 -NoWatch