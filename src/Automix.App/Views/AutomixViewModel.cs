using System.Collections.ObjectModel;

namespace Automix.App.Views;

public sealed record ChannelStripVm(string ChannelNumber, string ChannelName);

public sealed class AutomixViewModel
{
    public ObservableCollection<ChannelStripVm> Channels { get; } = new();

    public AutomixViewModel()
    {
        // Stage 0: placeholder channels
        for (int i = 1; i <= 16; i++)
            Channels.Add(new ChannelStripVm($"CH {i}", $"Mic {i}"));
    }
}
