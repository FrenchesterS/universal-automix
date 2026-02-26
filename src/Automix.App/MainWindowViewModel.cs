using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Automix.App;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private int _selectedTabIndex = 0;

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (value == _selectedTabIndex) return;
            _selectedTabIndex = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
