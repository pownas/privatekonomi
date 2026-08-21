namespace Privatekonomi.Web.Services;

public class ThemeService
{
    public event Action? OnThemeChanged;
    
    private bool _isDarkMode;
    
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                OnThemeChanged?.Invoke();
            }
        }
    }
    
    public void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
    }
}
