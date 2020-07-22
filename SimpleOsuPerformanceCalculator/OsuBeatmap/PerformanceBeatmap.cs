using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.IO;
using osu.Game.Rulesets.Objects;
using System.Collections.Generic;
using System.IO;

namespace SimpleOsuPerformanceCalculator.OsuBeatmap
{
    public class PerformanceBeatmap : WorkingBeatmap
    {
        public static Beatmap CreateFromFile(string beatmapFile)
        {
            using var reader = new LineBufferedReader(File.OpenRead(beatmapFile));
            var decoder = osu.Game.Beatmaps.Formats.Decoder.GetDecoder<Beatmap>(reader);
            return decoder.Decode(reader);
        }

        protected override Texture GetBackground() => null;

        protected override Track GetTrack() => null;

        private Beatmap FullBeatmap { get; }
        public PerformanceBeatmap(Beatmap beatmap) : base(beatmap.BeatmapInfo, null)
        {
            this.FullBeatmap = beatmap;
        }

        public double Offset { get; set; } = double.MaxValue;

        protected override IBeatmap GetBeatmap()
        {
            var lastHitObjectIndex = FullBeatmap.HitObjects.FindIndex((obj) => obj.StartTime >= Offset);
            if (FullBeatmap.HitObjects.Count - 1 == lastHitObjectIndex || lastHitObjectIndex == -1)
                return FullBeatmap;
            var tempBeatmap = FullBeatmap.Clone();
            tempBeatmap.HitObjects = new List<HitObject>(FullBeatmap.HitObjects);
            for (int i = FullBeatmap.HitObjects.Count - 1; i >= lastHitObjectIndex; i--)
            {
                tempBeatmap.HitObjects.RemoveAt(i);
            }
            return tempBeatmap;
        }
    }
}
