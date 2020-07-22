SimpleOsuPerformanceCalculator
---
A simple library to calculate performance point of osu!

## Usage
```csharp
var beatmapFilePath = @"";
var calculator = new SimplePerformanceCalculator(SupportModes.Osu, beatmapFilePath);

var maxCombo = Calculator.MaxCombo;
var max300 = Calculator.CurrentPlayingBeatmap.HitObjects.Count;

// get pp of current beatmap
calculator.UpdateOsuScore(max300, 0, 0, 0, 1, maxCombo);
Console.WriteLine(calculator.Performance);

// get pp on slice of Beatmap playable timeline
var offset = 10000; // ms, slice the hitobject which offset greater than settled value
calculator.SetCurrentOffset(offset);
Console.WriteLine(calculator.Performance);

// get pp with specific moderator(LegacyMods)
calculator.UpdateModerator(1 << 4); // in legacy moderator, '1 << 4' represent HR
Console.WriteLine(calculator.Performance);

```
