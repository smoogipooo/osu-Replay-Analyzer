﻿using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using BMAPI.v1;
using o_RA;
using ReplayAPI;

namespace ChartsPlugin
{
    public partial class TWChart : UserControl
    {
        public TWChart()
        {
            InitializeComponent();
        }

        Point LastHitPoint;

        private void TWChart_MouseClick(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Right:
                    Chart.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
                    Chart.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);
                    break;
                case MouseButtons.Left:
                    {
                        HitTestResult result = Chart.HitTest(e.X, e.Y);
                        if (result.ChartElementType == ChartElementType.DataPoint)
                        {
                            var point = Chart.Series[3].Points.FirstOrDefault(p => p.Color == oRAColours.Colour_Item_BG_0);
                            if (point != null)
                                point.Color = oRAColours.Colour_BG_P1;
                            Chart.Series[3].Points[result.PointIndex].Color = oRAColours.Colour_Item_BG_0;
                            oRA.Data.ChangeFrame(result.PointIndex);
                        }
                    }
                    break;
            }
        }
        private void TWChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (new Point(e.X, e.Y) == LastHitPoint)
                return;
            LastHitPoint = new Point(e.X, e.Y);
            HitTestResult result = Chart.HitTest(e.X, e.Y);
            if (result.PointIndex != -1 && result.Series != null && result.PointIndex < Chart.Series[3].Points.Count && Equals(result.Series, Chart.Series[3]))
            {
                    ChartToolTip.Tag = (int)Chart.Series[3].Points[result.PointIndex].XValue;
                    ChartToolTip.SetToolTip(Chart, Chart.Series[3].Points[result.PointIndex].YValues[0] + "ms");
            }
            else
            {
                ChartToolTip.Hide(Chart);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Chart.ChartAreas[0].AxisX.Title = oRA.Data.Language["oRA_CircleNumber"];
            Chart.ChartAreas[0].AxisY.Title = oRA.Data.Language["oRA_ErrorRateTime"];
            Chart.Series[0].Name = oRA.Data.Language["oRA_Hit50Region"];
            Chart.Series[1].Name = oRA.Data.Language["oRA_Hit100Region"];
            Chart.Series[2].Name = oRA.Data.Language["oRA_Hit300Region"];
            Chart.Series[3].LegendText = oRA.Data.Language["oRA_TimingWindow"];
            oRA.Data.ReplayChanged += HandleReplayChanged;
            oRA.Data.FrameChanged += HandleFrameChanged;
        }

        private void HandleReplayChanged(Replay r, Beatmap b)
        {
            Chart.SuspendLayout();
            Chart.Series[3].Points.Clear();
            Chart.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            Chart.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);
            Chart.ChartAreas[0].AxisY.Minimum = -oRA.Data.TimingWindows[0];
            Chart.ChartAreas[0].AxisY.Maximum = oRA.Data.TimingWindows[0];
            for (int i = 0; i < oRA.Data.ReplayObjects.Count; i++)
            {
                Chart.Series[3].Points.AddXY(i + 1, oRA.Data.ReplayObjects[i].Frame.Time - oRA.Data.ReplayObjects[i].Object.StartTime); 
            }         
            Chart.Series[0].Points.Clear();
            Chart.Series[0].Points.AddXY(0, oRA.Data.TimingWindows[2], -oRA.Data.TimingWindows[2]);
            Chart.Series[0].Points.AddXY(oRA.Data.ReplayObjects.Count, oRA.Data.TimingWindows[2], -oRA.Data.TimingWindows[2]);
            Chart.Series[1].Points.Clear();
            Chart.Series[1].Points.AddXY(0, oRA.Data.TimingWindows[1], -oRA.Data.TimingWindows[1]);
            Chart.Series[1].Points.AddXY(oRA.Data.ReplayObjects.Count, oRA.Data.TimingWindows[1], -oRA.Data.TimingWindows[1]);
            Chart.Series[2].Points.Clear();
            Chart.Series[2].Points.AddXY(0, oRA.Data.TimingWindows[0], -oRA.Data.TimingWindows[0]);
            Chart.Series[2].Points.AddXY(oRA.Data.ReplayObjects.Count, oRA.Data.TimingWindows[0], -oRA.Data.TimingWindows[0]);
            Chart.ResumeLayout();
        }
        private void HandleFrameChanged(int index)
        {
            if (index > Chart.Series[3].Points.Count - 1)
                return;
            var point = Chart.Series[3].Points.FirstOrDefault(p => p.Color == oRAColours.Colour_Item_BG_0);
            if (point != null)
                point.Color = oRAColours.Colour_BG_P1;
            Chart.Series[3].Points[index].Color = oRAColours.Colour_Item_BG_0;
        }
    }
}
