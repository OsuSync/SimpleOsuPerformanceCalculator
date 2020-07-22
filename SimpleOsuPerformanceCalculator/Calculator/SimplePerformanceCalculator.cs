using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
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
            MaxCombo = beatmap.HitObjects.Count;
            if (Mode == SupportModes.Osu)
            {
                MaxCombo += beatmap.HitObjects.OfType<Slider>().Sum(s => s.NestedHitObjects.Count - 1);
            }
        } 

        public void UpdateScore(ScoreInfo score)
        {
            typeof(PerformanceCalculator).GetType().GetField("Score", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(LazerCalculator, score);
            Refresh();
        }

        public void UpdateOsuScore(int n300, int n100, int n50, int miss, double acc, int maxCombo)
        {
            Score.MaxCombo = maxCombo;
            Score.SetCount300(n300);
            Score.SetCount100(n100);
            Score.SetCount50(n50);
            Score.SetCountMiss(miss);
            Score.Accuracy = acc;
            Score.MaxCombo = maxCombo;
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

            LazerCalculator.GetType()
                .GetField("countHitCircles", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(LazerCalculator, CurrentPlayingBeatmap.HitObjects.Count);

            RefreshMaxCombo(CurrentPlayingBeatmap);
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
