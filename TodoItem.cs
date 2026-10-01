using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace FastTodo;

public sealed class TodoItem : INotifyPropertyChanged
{
    string _name = "";
    double _percent;
    string? _percentText; // raw text while the user is typing

    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; Raise(); } }
    }

    public double Percent
    {
        get => _percent;
        set
        {
            value = double.IsFinite(value) ? Math.Clamp(Math.Round(value, 3), 0, 100) : 0;
            if (_percent == value) return;
            _percent = value;
            Raise();
            Raise(nameof(IsDone));
        }
    }

    [JsonIgnore] public bool IsDone => _percent >= 100;

    // Bound to the percent box: accepts "25", "12.5" or "12,5" and updates Percent live.
    [JsonIgnore]
    public string PercentText
    {
        get => _percentText ?? _percent.ToString("0.###", CultureInfo.InvariantCulture);
        set
        {
            _percentText = value;
            var s = value.Replace(',', '.').Trim();
            if (s.Length == 0) Percent = 0;
            else if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var p)) Percent = p;
        }
    }

    // Called when editing ends: show the clean, clamped value again.
    public void NormalizeText() { _percentText = null; Raise(nameof(PercentText)); }

    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
