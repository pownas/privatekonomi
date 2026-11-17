# Windows – lokal installation och start

Kör PowerShell från repositoryts rot:

```powershell
.\scripts\windows\install.ps1
.\scripts\windows\start.ps1
```

Starta utan hot reload med `-NoWatch`:

```powershell
.\scripts\windows\start.ps1 -NoWatch
```

De äldre kommandona `local-app-install.ps1` och `local-app-start.ps1` finns kvar som kompatibilitetsstartare i repositoryts rot.