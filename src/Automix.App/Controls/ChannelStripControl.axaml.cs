using Avalonia.Controls;
using Avalonia.Input;
using Automix.App.Views;

namespace Automix.App.Controls;

public partial class ChannelStripControl : UserControl
{
    public ChannelStripControl()
    {
        InitializeComponent();
    }

    private void OnMeterWheel(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not ChannelStripViewModel vm) return;

        if (e.Delta.Y > 0) vm.PriorityDb += 0.5;
        else if (e.Delta.Y < 0) vm.PriorityDb -= 0.5;

        e.Handled = true; // важно: чтобы не скроллило горизонтально
    }
}