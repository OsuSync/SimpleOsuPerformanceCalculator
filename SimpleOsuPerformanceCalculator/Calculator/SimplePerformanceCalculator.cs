using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using SimpleOsuPerformanceCalculator.OsuBeatmap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SimpleOsuPerformanceCalculator.Calculator
{
    public enum SupportModes
    {
        Osu, Mania, Taiko, Catch
    }
    public class SimplePerformanceCalculator
    {
        protected static Dictionary<SupportModes, Ruleset> DefaultRuleSets = new Dictionary<SupportModes, Ruleset>()
        {
            { SupportModes.Osu, new OsuRuleset() },
            { SupportModes.Mania, new ManiaRuleset() },
            { SupportModes.Taiko, new TaikoRuleset() },
            { SupportModes.Catch, new CatchRuleset() },
        };
        public event Action<double> PerformanceChanged;

        private static Ruleset GetRuleset(SupportModes mode)
        {
            return DefaultRuleSets[mode];
        }
        private PerformanceCalculator LazerCalculator { get; }
        public Beatmap OsuBeatmap { get; }
        public PerformanceBeatmap PpBeatmap { get; }
        public IBeatmap CurrentPlayingBeatmap { get; private set; }
        private ScoreInfo Score { get; set; }
        public double Performance { get; private set; }
        public int MaxCombo { get; private set; }
        public int Max300 { get; private set; }
        private Ruleset Ruleset { get; }
        public SupportModes Mode { get; private set; }
        public SimplePerformanceCalculator(SupportModes mode, string beatmapFile)
        {
            Mode = mode;
            OsuBeatmap = PerformanceBeatmap.CreateFromFile(beatmapFile);
            PpBeatmap = new PerformanceBeatmap(OsuBeatmap);
            Score = new ScoreInfo();
            Ruleset = GetRuleset(Mode);
            LazerCalculator = Ruleset.CreatePerformanceCalculator(PpBeatmap, Score);
            CurrentPlayingBeatmap = PpBeatmap.GetPlayableBeatmap(Ruleset.RulesetInfo, Score.Mods);
            RefreshMaxCombo(CurrentPlayingBeatmap);
        }

        private void RefreshMaxCombo(IBeatmap beatmap)
        {
            Max300 = beatmap.HitObjects.Count;
            if (Mode == SupportModes.Osu)
            {
                MaxCombo = Max300 + beatmap.HitObjects.OfType<Slider>().Sum(s => s.NestedHitObjects.Count - 1);
            }
        } 

        public void UpdateScore(ScoreInfo score)
        {
            typeof(PerformanceCalculator).GetType().GetField("Score", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(LazerCalculator, score);
            Refresh();
        }

        public void UpdateOsuScore(int? n300, int? n100, int? n50, int? miss, double? acc, int? maxCombo)
        {
            if (n300.HasValue) Score.SetCount300(n300.Value);
            if (n100.HasValue) Score.SetCount100(n100.Value);
            if (n50.HasValue) Score.SetCount50(n50.Value);
            if (miss.HasValue) Score.SetCountMiss(miss.Value);
            if (acc.HasValue) Score.Accuracy = acc.Value;
            if (maxCombo.HasValue) Score.MaxCombo = maxCombo.Value;
            Refresh();
        }

        public void UpdateManiaScore(long? score, int? perfect, int? great, int? good, int? ok, int? meh, int? miss, double? acc, int? maxCombo)
        {
            if (perfect.HasValue) Score.Statistics[HitResult.Perfect] = perfect.Value;
            if (ok.HasValue) Score.Statistics[HitResult.Ok] = ok.Value;
            if (good.HasValue) Score.Statistics[HitResult.Good] = good.Value;
            if (great.HasValue) Score.Statistics[HitResult.Great] = great.Value;
            if (meh.HasValue) Score.Statistics[HitResult.Meh] = meh.Value;
            if (miss.HasValue) Score.SetCountMiss(miss.Value);
            if (score.HasValue) Score.TotalScore = score.Value;
            if (acc.HasValue) Score.Accuracy = acc.Value;
            if (maxCombo.HasValue) Score.MaxCombo = maxCombo.Value;
            Refresh();
        }

        public void UpdateTaikoScore(int? n300, int? n100, int? n50, int? miss)
        {
            if (n300.HasValue) Score.SetCount300(n300.Value);
            if (n100.HasValue) Score.SetCount100(n100.Value);
            if (n50.HasValue) Score.SetCount50(n50.Value);
            if (miss.HasValue) Score.SetCountMiss(miss.Value);
            Refresh();
        }

        public void UpdateCatchScore(int? perfect, int? largeTickHit, int? smallTickHit, int? smallTickMiss, int? miss)
        {
            if (perfect.HasValue) Score.SetCount300(perfect.Value);
            if (largeTickHit.HasValue) Score.Statistics[HitResult.LargeTickHit] = largeTickHit.Value;
            if (smallTickHit.HasValue) Score.Statistics[HitResult.SmallTickHit] = smallTickHit.Value;
            if (smallTickMiss.HasValue) Score.Statistics[HitResult.SmallTickMiss] = smallTickMiss.Value;
            if (miss.HasValue) Score.SetCountMiss(miss.Value);
            Refresh();
        }

        public void UpdateModerator(LegacyMods moderator)
        {
            Score.Mods = Ruleset.ConvertFromLegacyMods(moderator).ToArray();
            Refresh();
        }
        public void UpdateModerator(int rawModerator)
        {
            UpdateModerator((LegacyMods)rawModerator);
            RefreshBeatmap();
            Refresh();
        }

        private void RefreshBeatmap()
        {

            typeof(WorkingBeatmap)
                .GetField("beatmapLoadTask", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(PpBeatmap, null);

            CurrentPlayingBeatmap = PpBeatmap.GetPlayableBeatmap(Ruleset.RulesetInfo, Score.Mods);
            typeof(PerformanceCalculator)
                .GetField("Beatmap", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(LazerCalculator, CurrentPlayingBeatmap);

            var nextAttributes = Ruleset.CreateDifficultyCalculator(PpBeatmap).Calculate(Score.Mods);
            typeof(PerformanceCalculator)
                .GetField("Attributes", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(LazerCalculator, nextAttributes);

            RefreshMaxCombo(CurrentPlayingBeatmap);

            LazerCalculator.GetType()
                .GetField("countHitCircles", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(LazerCalculator, Max300);

            LazerCalculator.GetType()
                .GetField("beatmapMaxCombo", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(LazerCalculator, MaxCombo);

            typeof(PerformanceCalculator).GetMethod("ApplyMods", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(LazerCalculator, new[] { Score.Mods });
        }

        public void SetCurrentOffset(double offset)
        {
            PpBeatmap.Offset = offset;
            RefreshBeatmap();
            Refresh();
        }

        public void Refresh()
        {
            var pp = LazerCalculator.Calculate();
            if (Performance != pp)
            {
                PerformanceChanged?.Invoke(pp);
                this.Performance = pp;
            }
        }
    }
}
