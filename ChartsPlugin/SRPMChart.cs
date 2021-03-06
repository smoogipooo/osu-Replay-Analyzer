﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using BMAPI;
using BMAPI.v1;
using BMAPI.v1.HitObjects;
using ReplayAPI;

namespace ChartsPlugin
{
    public partial class SRPMChart : UserControl
    {
        public SRPMChart()
        {
            InitializeComponent();
        }

        Point LastHitPoint;

        private void SRPMChart_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Chart.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
                Chart.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);
            }
        }

        private void SRPMChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (new Point(e.X, e.Y) == LastHitPoint)
                return;
            LastHitPoint = new Point(e.X, e.Y);
            HitTestResult result = Chart.HitTest(e.X, e.Y);
            if (result.PointIndex != -1 && result.Series != null && result.Series.BorderWidth != 0)
            {
                result.Series.BorderWidth = 3;
            }
            else
            {
                foreach (Series s in Chart.Series.Where(s => !Equals(s.Tag, "1") && s.BorderWidth != 0 && s.BorderWidth != 2))
                {
                    Chart.Series[Chart.Series.IndexOf(s)].BorderWidth = 2;
                }
            }

            if (result.PointIndex != -1 && result.Series != null && result.PointIndex < result.Series.Points.Count && !Equals(result.Series.Tag, "0"))
            {
                ChartToolTip.SetToolTip(Chart, result.Series.Points[result.PointIndex].YValues[0] + oRA.Data.Language["oRA_RPM"]);
            }
            else
            {
                ChartToolTip.Hide(Chart);
            }
        }

        private void SRPMChart_MouseDown(object sender, MouseEventArgs e)
        {
            HitTestResult result = Chart.HitTest(e.X, e.Y);
            if (result.PointIndex != -1 && result.Series != null)
            {
                if (result.Series.BorderWidth == 0)
                {
                    foreach (Series s in Chart.Series)
                    {
                        Chart.Series[Chart.Series.IndexOf(s)].BorderWidth = 2;
                        Chart.Series[Chart.Series.IndexOf(s)].IsVisibleInLegend = true;
                        Chart.Series[Chart.Series.IndexOf(s)].Tag = "";
                    }
                }
                else
                {
                    foreach (Series s in Chart.Series.Where(s => !Equals(s, result.Series)))
                    {
                        Chart.Series[Chart.Series.IndexOf(s)].BorderWidth = 0;
                        Chart.Series[Chart.Series.IndexOf(s)].IsVisibleInLegend = false;
                        Chart.Series[Chart.Series.IndexOf(s)].Tag = "0";
                    }
                    result.Series.BorderWidth = 3;
                    result.Series.Tag = "1";
                }

            }
            else
            {
                foreach (Series s in Chart.Series)
                {
                    Chart.Series[Chart.Series.IndexOf(s)].BorderWidth = 2;
                    Chart.Series[Chart.Series.IndexOf(s)].IsVisibleInLegend = true;
                    Chart.Series[Chart.Series.IndexOf(s)].Tag = "";
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Chart.ChartAreas[0].AxisX.Title = oRA.Data.Language["oRA_MapTime"];
            Chart.ChartAreas[0].AxisY.Title = oRA.Data.Language["oRA_RPM"];
            oRA.Data.ReplayChanged += HandleReplayChanged;
        }

        private void HandleReplayChanged(Replay r, Beatmap b)
        {
            Chart.SuspendLayout();
            Chart.Series.Clear();
            Chart.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            Chart.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);
            int currentSpinnerNumber = 1;
            foreach (var spinner in b.HitObjects.Where(o => o.GetType() == typeof(SpinnerObject)))
            {
                Point2 currentPosition = new Point2(-500, -500);
                Dictionary<double, int> RPMCount = new Dictionary<double, int>();
                double currentTime = 0;
                foreach (ReplayInfo repPoint in r.ReplayFrames.Where(repPoint => repPoint.Time < ((SpinnerObject)spinner).EndTime && repPoint.Time > spinner.StartTime))
                {
                    if ((int)currentPosition.X == -500)
                    {
                        currentPosition.X = repPoint.X;
                        currentPosition.Y = repPoint.Y;
                    }
                    else
                    {
                        currentTime += repPoint.TimeDiff;
                        if (RPMCount.Keys.Contains(currentTime))
                            continue;
                        double ptsDist = currentPosition.DistanceTo(new Point2(repPoint.X, repPoint.Y));
                        double p1CDist = currentPosition.DistanceTo(spinner.Location);
                        double p2CDist = new Point2(repPoint.X, repPoint.Y).DistanceTo(spinner.Location);
                        double travelDegrees = Math.Acos((Math.Pow(p1CDist, 2) + Math.Pow(p2CDist, 2) - Math.Pow(ptsDist, 2)) / (2 * p1CDist * p2CDist)) * (180 / Math.PI);
                        RPMCount.Add(currentTime, (int)Math.Min((travelDegrees / (0.006 * repPoint.TimeDiff)), 477));
                        currentPosition.X = repPoint.X;
                        currentPosition.Y = repPoint.Y;
                    }
                }
                int count = 0;
                int valueAmnt = 0;
                Series spinnerSeries = new Series
                {
                    ChartType = SeriesChartType.Spline,
                    BorderWidth = 2,
                    Name = oRA.Data.Language["oRA_Spinner"] + " " + currentSpinnerNumber
                };
                foreach (var frame in RPMCount)
                {
                    valueAmnt += frame.Value;
                    count += 1;
                    if (count == 5)
                    {
                        spinnerSeries.Points.AddXY(frame.Key, Convert.ToInt32(valueAmnt / count));
                        count = 0;
                        valueAmnt = 0;
                    }
                }
                Chart.Series.Add(spinnerSeries);
                currentSpinnerNumber += 1;
            }
            Chart.ResumeLayout();
        }
    }
}
