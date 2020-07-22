using NUnit.Framework;
using SimpleOsuPerformanceCalculator.Calculator;
using System;
using System.Diagnostics;

namespace SimpleOsuPerformanceCalculator.Test
{
    public class CalculateTest
    {
        public SimplePerformanceCalculator CreateCalculator()
        {
            var path = @"Mai zang - Ying Huo Chong Zhi Yuan (- Remilia) [AR10].osu";
            return new SimplePerformanceCalculator(SupportModes.Osu, path);
        }

        [Test]
        public void BasicPerformaceCalcTest()
        {
            var Calculator = CreateCalculator();
            Calculator.UpdateOsuScore(Calculator.CurrentPlayingBeatmap.HitObjects.Count, 0, 0, 0, 1, Calculator.MaxCombo);
            // 278.44709436780693
            var pp1 = Calculator.Performance;
            
            Calculator.SetCurrentOffset(110963);
            Calculator.UpdateOsuScore(Calculator.CurrentPlayingBeatmap.HitObjects.Count, 0, 0, 0, 1, Calculator.MaxCombo);
            // 264.76845817826575
            var pp2 = Calculator.Performance;
            Calculator.UpdateModerator(1 << 4); // represent HR
            var pp3 = Calculator.Performance;
            Calculator.SetCurrentOffset(double.MaxValue);
            Calculator.UpdateOsuScore(Calculator.CurrentPlayingBeatmap.HitObjects.Count, 0, 0, 0, 1, Calculator.MaxCombo);
            var pp4 = Calculator.Performance;

            Assert.AreNotEqual(pp1, pp2);
            Assert.Greater(pp3, pp2, "pp3 > pp2");
            Assert.Greater(pp4, pp3, "pp4 > pp3");
            Assert.Greater(pp4, pp1, "pp4 > pp1");
            Assert.IsTrue(Math.Abs(pp1 - 278.44709436780693) < 0.001);
            Assert.IsTrue(Math.Abs(pp2 - 264.76845817826575) < 0.001);
        }

        [Test]
        public void SimulatePlayTimeLine()
        {
            var Calculator = CreateCalculator();
            var rand = new Random();

            Calculator.UpdateModerator(1 << 4); // represent HR


            Calculator.UpdateOsuScore(Calculator.CurrentPlayingBeatmap.HitObjects.Count, 0, 0, 0, 1, Calculator.MaxCombo);
            var pp1 = Calculator.Performance;


            for (int i = 0; i < 153564; i += rand.Next(500, 800))
            {
                Calculator.SetCurrentOffset(i);
                Calculator.UpdateOsuScore(Calculator.CurrentPlayingBeatmap.HitObjects.Count, 0, 0, 0, 1, Calculator.MaxCombo);
                Debug.Write($"Offset:{i}, MaxCombo: {Calculator.MaxCombo}, PP: {Calculator.Performance}");
            }
            var pp2 = Calculator.Performance;


            Assert.AreEqual(pp1, pp2);
        }
    }
}
