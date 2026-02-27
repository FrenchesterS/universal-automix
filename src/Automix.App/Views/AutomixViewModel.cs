using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;

namespace Automix.App.Views;

public enum FilterTab { All, A, B, C, D }

public sealed class ChannelStripViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public int ChannelNumber { get; }
    public string Name { get; }
    public string? IconKey { get; }

    public bool InA { get => _inA; set { if (Set(ref _inA, value)) RecomputeBrushRequested?.Invoke(); } }
    public bool InB { get => _inB; set { if (Set(ref _inB, value)) RecomputeBrushRequested?.Invoke(); } }
    public bool InC { get => _inC; set { if (Set(ref _inC, value)) RecomputeBrushRequested?.Invoke(); } }
    public bool InD { get => _inD; set { if (Set(ref _inD, value)) RecomputeBrushRequested?.Invoke(); } }

    public bool IsMuted { get => _isMuted; set { if (Set(ref _isMuted, value)) RecomputeBrushRequested?.Invoke(); } }

    public double PriorityDb
    {
        get => _priorityDb;
        set
        {
            var v = Math.Clamp(value, -12, 12);
            if (Set(ref _priorityDb, v)) RecomputeBrushRequested?.Invoke();
        }
    }

    public double Level { get => _level; set => Set(ref _level, Math.Clamp(value, 0, 1)); }
    public double Attenuation { get => _attenuation; set => Set(ref _attenuation, Math.Clamp(value, 0, 1)); }
    public bool IsActiveTalker { get => _isActiveTalker; set => Set(ref _isActiveTalker, value); }

    public IBrush MeterBrush { get => _meterBrush; set => Set(ref _meterBrush, value); }

    internal Action? RecomputeBrushRequested { get; set; }

    private bool _inA, _inB, _inC, _inD;
    private bool _isMuted;
    private double _priorityDb;
    private double _level;
    private double _attenuation;
    private bool _isActiveTalker;
    private IBrush _meterBrush = Brushes.Gray;

    public ChannelStripViewModel(int channelNumber, string name, string? iconKey = null)
    {
        ChannelNumber = channelNumber;
        Name = name;
        IconKey = iconKey;
    }

    public bool IsInAnyGroup() => InA || InB || InC || InD;

    public bool IsInFilter(FilterTab tab) => tab switch
    {
        FilterTab.All => true,
        FilterTab.A => InA,
        FilterTab.B => InB,
        FilterTab.C => InC,
        FilterTab.D => InD,
        _ => true
    };

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}

public sealed class AutomixViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ChannelStripViewModel> VisibleChannels { get; } = new();
    private readonly ObservableCollection<ChannelStripViewModel> _all = new();

    public FilterTab ActiveTab
    {
        get => _activeTab;
        set
        {
            if (Set(ref _activeTab, value))
            {
                // HideInactive forced ON for group tabs
                if (_activeTab != FilterTab.All)
                {
                    _hideInactive = true;
                    OnPropertyChanged(nameof(HideInactive));
                }
                RecomputeVisibility();
                RecomputeBrushes();
                OnPropertyChanged(nameof(HideInactiveEnabled));
            }
        }
    }

    public bool HideInactive
    {
        get => _hideInactive;
        set
        {
            if (ActiveTab != FilterTab.All) return; // locked
            if (Set(ref _hideInactive, value)) RecomputeVisibility();
        }
    }

    public bool HideInactiveEnabled => ActiveTab == FilterTab.All;

    private FilterTab _activeTab = FilterTab.All;
    private bool _hideInactive;

    // Simulation
    private readonly DispatcherTimer _tick;
    private readonly Random _rng = new(1);
    private int _talkerIndex;
    private int _holdTicks;

    public AutomixViewModel()
    {
        for (int i = 1; i <= 32; i++)
        {
            var ch = new ChannelStripViewModel(i, $"Mic {i}");
            ch.RecomputeBrushRequested = () =>
            {
                RecomputeVisibility();
                RecomputeBrushes();
            };
            _all.Add(ch);
        }

        RecomputeVisibility();
        RecomputeBrushes();

        _tick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _tick.Tick += (_, __) => TickSim();
        _tick.Start();
    }

    private void TickSim()
    {
        // смена говорящего каждые ~2 сек (50мс * 40)
        _holdTicks++;
        if (_holdTicks >= 40)
        {
            _holdTicks = 0;
            _talkerIndex = (_talkerIndex + 1) % _all.Count;
        }

        var talker = _all[_talkerIndex];

        foreach (var ch in _all)
        {
            ch.IsActiveTalker = false;
            ch.Level = 0.08 + _rng.NextDouble() * 0.05;
            ch.Attenuation = 0.0;
        }

        talker.IsActiveTalker = true;
        talker.Level = 0.65 + _rng.NextDouble() * 0.20;

        // очень простая имитация автомикса (визуальная): глушим остальных в группах говорящего
        ApplyAttenuationIfSameGroup(talker, c => talker.InA && c.InA);
        ApplyAttenuationIfSameGroup(talker, c => talker.InB && c.InB);
        ApplyAttenuationIfSameGroup(talker, c => talker.InC && c.InC);
        ApplyAttenuationIfSameGroup(talker, c => talker.InD && c.InD);

        // muted: не участвует, визуально “мертвый”
        foreach (var ch in _all)
        {
            if (!ch.IsMuted) continue;
            ch.Level = 0.0;
            ch.Attenuation = 1.0;
            ch.IsActiveTalker = false;
        }
    }

    private void ApplyAttenuationIfSameGroup(ChannelStripViewModel talker, Func<ChannelStripViewModel, bool> membership)
    {
        // если говорящий не в этой группе — ничего
        if (!membership(talker)) return;

        foreach (var ch in _all)
        {
            if (ch.IsMuted) continue;
            if (!membership(ch)) continue;
            if (ch == talker) continue;

            ch.Attenuation = Math.Max(ch.Attenuation, 0.65); // overlay
            ch.Level = Math.Min(ch.Level, 0.20 + _rng.NextDouble() * 0.05);
        }
    }

    private void RecomputeVisibility()
    {
        VisibleChannels.Clear();

        foreach (var ch in _all)
        {
            if (!ch.IsInFilter(ActiveTab)) continue;

            if (ActiveTab == FilterTab.All)
            {
                if (HideInactive && !ch.IsInAnyGroup()) continue;
            }
            else
            {
                // В группе всегда hide inactive и так включён
                if (!ch.IsInAnyGroup()) continue;
            }

            VisibleChannels.Add(ch);
        }
    }

    private void RecomputeBrushes()
    {
        // берём цвета из ресурсов темы
        var app = Avalonia.Application.Current;

        var a = TryBrush(app, "GroupAColor", Colors.DeepSkyBlue);
        var b = TryBrush(app, "GroupBColor", Colors.MediumSeaGreen);
        var c = TryBrush(app, "GroupCColor", Colors.Orange);
        var d = TryBrush(app, "GroupDColor", Colors.MediumPurple);
        var none = new SolidColorBrush(Color.FromRgb(90, 90, 90));

        foreach (var ch in _all)
        {
            if (ActiveTab == FilterTab.All)
            {
                var list = new System.Collections.Generic.List<IBrush>();
                if (ch.InA) list.Add(a);
                if (ch.InB) list.Add(b);
                if (ch.InC) list.Add(c);
                if (ch.InD) list.Add(d);

                if (list.Count == 0) ch.MeterBrush = none;
                else if (list.Count == 1) ch.MeterBrush = list[0];
                else ch.MeterBrush = MakeVerticalGradient(list);
            }
            else
            {
                ch.MeterBrush = ActiveTab switch
                {
                    FilterTab.A => a,
                    FilterTab.B => b,
                    FilterTab.C => c,
                    FilterTab.D => d,
                    _ => none
                };
            }
        }
    }

private static IBrush TryBrush(Avalonia.Application? app, string key, Color fallback)
{
    if (app?.Resources is not null && app.Resources.TryGetValue(key, out var v) && v is IBrush br)
        return br;

    return new SolidColorBrush(fallback);
}

private static IBrush MakeVerticalGradient(System.Collections.Generic.IReadOnlyList<IBrush> brushes)
{
    var colors = brushes
        .Select(b => b is SolidColorBrush sb ? sb.Color : Colors.White)
        .ToArray();

    var g = new LinearGradientBrush
    {
        StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
	EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative)
    };

    var n = colors.Length;
    for (int i = 0; i < n; i++)
    {
        var t0 = (double)i / n;
        var t1 = (double)(i + 1) / n;
        g.GradientStops.Add(new GradientStop(colors[i], t0));
        g.GradientStops.Add(new GradientStop(colors[i], t1));
    }

    return g;
}

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}