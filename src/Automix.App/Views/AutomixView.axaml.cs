using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Automix.App.Views;

public partial class AutomixView : UserControl
{
    public AutomixView()
    {
        InitializeComponent();
    }

    private AutomixViewModel? Vm => DataContext as AutomixViewModel;

    private void OnTabClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is null) return;
        if (sender is not ToggleButton tb) return;

        var tag = tb.Tag?.ToString() ?? "All";
        Vm.ActiveTab = tag switch
        {
            "A" => FilterTab.A,
            "B" => FilterTab.B,
            "C" => FilterTab.C,
            "D" => FilterTab.D,
            _ => FilterTab.All
        };

        // exclusive tabs
        TabAll.IsChecked = Vm.ActiveTab == FilterTab.All;
        TabA.IsChecked = Vm.ActiveTab == FilterTab.A;
        TabB.IsChecked = Vm.ActiveTab == FilterTab.B;
        TabC.IsChecked = Vm.ActiveTab == FilterTab.C;
        TabD.IsChecked = Vm.ActiveTab == FilterTab.D;
    }

    private void OnChannelScrollWheel(object? sender, PointerWheelEventArgs e)
    {
        if (e.Handled) return; // meter wheel used for priority
        if (sender is not ScrollViewer sv) return;

        var dx = -e.Delta.Y * 40; // скорость скролла
        var off = sv.Offset;
        sv.Offset = new Vector(off.X + dx, off.Y);
        e.Handled = true;
    }
}