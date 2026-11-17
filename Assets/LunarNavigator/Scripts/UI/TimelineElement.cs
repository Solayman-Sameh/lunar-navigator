using System;
using System.Collections.Generic;
using System.Globalization;
using LunarNavigator.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace LunarNavigator.UI
{
    public class TimelineElement : VisualElement
    {
        public event Action<double> OnPlayheadDragged;

        private SiteAnalysis currentSite;
        private double currentTimeSec;
        private double landingTimeSec;
        private double missionDurationSec = 24.0 * 3600.0;
        private bool isDraggingPlayhead = false;

        // Colors
        private readonly Color colSun = new Color(0.96f, 0.71f, 0.26f, 0.95f);
        private readonly Color colEarth = new Color(0.35f, 0.66f, 0.96f, 0.95f);
        private readonly Color colBoth = new Color(0.44f, 0.89f, 0.69f, 0.95f);
        private readonly Color colTrackBg = new Color(0.12f, 0.16f, 0.22f, 0.75f);
        private readonly Color colBracketBg = new Color(0.44f, 0.89f, 0.69f, 0.15f);
        private readonly Color colBracketBorder = new Color(0.44f, 0.89f, 0.69f, 0.85f);
        private readonly Color colPlayhead = new Color(1f, 1f, 1f, 0.98f);

        // UI hierarchy elements
        private Label lblCoverageRange;
        private Label lblScopeType;
        private VisualElement lanesContainer;
        private VisualElement tracksArea;
        private VisualElement windowBracket;
        private VisualElement playheadLine;
        private VisualElement playheadCapTop;
        private VisualElement playheadCapBottom;
        private Label lblHoverStatus;

        private const float LabelWidth = 105f;
        private const float RowHeight = 20f;
        private const float RowGap = 3f;
        private const string DefaultHoverPrompt = "Click or drag timeline to scrub simulation time · Hover over segments for exact UTC duration";

        public TimelineElement()
        {
            style.height = 118;
            style.width = Length.Percent(100);
            style.flexDirection = FlexDirection.Column;
            style.paddingTop = 4;
            style.paddingBottom = 4;
            style.paddingLeft = 4;
            style.paddingRight = 4;

            BuildStructure();
        }

        private void BuildStructure()
        {
            Clear();

            // 1. Top Header: Exact UTC Start/End Timestamps and Scope Declaration
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 4;

            lblCoverageRange = new Label("COVERAGE: -- UTC → -- UTC (-- h)");
            lblCoverageRange.style.fontSize = 11;
            lblCoverageRange.style.color = new Color(0.85f, 0.90f, 0.96f, 0.95f);
            lblCoverageRange.style.letterSpacing = 0.5f;
            headerRow.Add(lblCoverageRange);

            lblScopeType = new Label("SCOPE: Selected mission window");
            lblScopeType.style.fontSize = 11;
            lblScopeType.style.unityFontStyleAndWeight = FontStyle.Bold;
            lblScopeType.style.color = new Color(0.44f, 0.89f, 0.69f, 0.95f);
            headerRow.Add(lblScopeType);

            Add(headerRow);

            // 2. Lanes and Tracks Area
            lanesContainer = new VisualElement();
            lanesContainer.style.flexDirection = FlexDirection.Column;
            lanesContainer.style.position = Position.Relative;
            Add(lanesContainer);

            // 3. Bottom Status / Hover Readout Bar
            lblHoverStatus = new Label(DefaultHoverPrompt);
            lblHoverStatus.style.fontSize = 11;
            lblHoverStatus.style.color = new Color(0.55f, 0.63f, 0.72f);
            lblHoverStatus.style.marginTop = 4;
            lblHoverStatus.style.unityTextAlign = TextAnchor.MiddleCenter;
            Add(lblHoverStatus);
        }

        public void SetData(SiteAnalysis site, double currentSec, double landingSec, double durationHours)
        {
            currentSite = site;
            currentTimeSec = currentSec;
            landingTimeSec = landingSec;
            missionDurationSec = durationHours * 3600.0;
            RebuildLanes();
        }

        public void SetCurrentTime(double currentSec)
        {
            currentTimeSec = currentSec;
            UpdatePlayhead();
        }

        private void RebuildLanes()
        {
            lanesContainer.Clear();

            if (currentSite?.ephemeris == null || currentSite.visibility == null || currentSite.ephemeris.n <= 1)
            {
                lblCoverageRange.text = "COVERAGE: No ephemeris data loaded";
                lblScopeType.text = "SCOPE: Idle";
                return;
            }

            int n = currentSite.ephemeris.n;
            int step_s = currentSite.ephemeris.step_s;
            double totalSec = (n - 1) * (double)step_s;
            double totalHours = totalSec / 3600.0;

            DateTime t0Utc = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(currentSite.ephemeris.t0_utc))
            {
                if (DateTime.TryParse(currentSite.ephemeris.t0_utc, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
                {
                    t0Utc = parsed;
                }
            }
            DateTime tEndUtc = t0Utc.AddSeconds(totalSec);

            // Exact UTC coverage display
            lblCoverageRange.text = $"COVERAGE: {t0Utc:yyyy-MM-dd HH:mm:ss} UTC → {tEndUtc:yyyy-MM-dd HH:mm:ss} UTC ({totalHours:F1} h)";

            // Clearly state whether timeline shows selected mission window or wider coverage
            bool isExactMissionWindow = Math.Abs(missionDurationSec - totalSec) < 60.0 && landingTimeSec < 60.0;
            if (isExactMissionWindow)
            {
                lblScopeType.text = "SCOPE: Selected mission window";
                lblScopeType.style.color = new Color(0.44f, 0.89f, 0.69f, 0.95f);
            }
            else
            {
                lblScopeType.text = $"SCOPE: Wider analysis coverage (mission window: {missionDurationSec / 3600.0:F1} h)";
                lblScopeType.style.color = new Color(0.35f, 0.66f, 0.96f, 0.95f);
            }

            // Container holding the 3 track lines
            tracksArea = new VisualElement { name = "timeline-tracks-area" };
            tracksArea.style.flexDirection = FlexDirection.Column;
            tracksArea.style.position = Position.Relative;
            tracksArea.style.flexGrow = 1;

            // Register scrubber events on tracks area
            tracksArea.RegisterCallback<PointerDownEvent>(OnTracksPointerDown);
            tracksArea.RegisterCallback<PointerMoveEvent>(OnTracksPointerMove);
            tracksArea.RegisterCallback<PointerUpEvent>(OnTracksPointerUp);
            tracksArea.RegisterCallback<PointerLeaveEvent>(evt => {
                if (!isDraggingPlayhead) lblHoverStatus.text = DefaultHoverPrompt;
            });

            // Compute Both visibility array
            int[] bothVis = new int[n];
            for (int i = 0; i < n; i++)
            {
                int s = (currentSite.visibility.sun_visible != null && i < currentSite.visibility.sun_visible.Length) ? currentSite.visibility.sun_visible[i] : 0;
                int e = (currentSite.visibility.earth_visible != null && i < currentSite.visibility.earth_visible.Length) ? currentSite.visibility.earth_visible[i] : 0;
                bothVis[i] = (s == 1 && e == 1) ? 1 : 0;
            }

            // Row 1: SUNLIGHT
            tracksArea.Add(CreateLaneRow("SUNLIGHT", currentSite.visibility.sun_visible, colSun, t0Utc, step_s, n));
            // Row 2: EARTH VISIBLE
            tracksArea.Add(CreateLaneRow("EARTH VISIBLE", currentSite.visibility.earth_visible, colEarth, t0Utc, step_s, n));
            // Row 3: BOTH VISIBLE
            tracksArea.Add(CreateLaneRow("BOTH VISIBLE", bothVis, colBoth, t0Utc, step_s, n));

            // Mission Window Bracket Overlay
            windowBracket = new VisualElement { name = "window-bracket", pickingMode = PickingMode.Ignore };
            windowBracket.style.position = Position.Absolute;
            windowBracket.style.top = 0;
            windowBracket.style.bottom = 0;
            windowBracket.style.backgroundColor = Color.clear;
            windowBracket.style.borderLeftWidth = 2f;
            windowBracket.style.borderRightWidth = 2f;
            windowBracket.style.borderLeftColor = colBracketBorder;
            windowBracket.style.borderRightColor = colBracketBorder;

            float winStartU = Mathf.Clamp01((float)(landingTimeSec / totalSec));
            float winEndU = Mathf.Clamp01((float)((landingTimeSec + missionDurationSec) / totalSec));
            windowBracket.style.left = Length.Percent(winStartU * 100f);
            windowBracket.style.width = Length.Percent(Mathf.Max(0.5f, (winEndU - winStartU) * 100f));
            tracksArea.Add(windowBracket);

            // Playhead
            playheadLine = new VisualElement { name = "playhead-line", pickingMode = PickingMode.Ignore };
            playheadLine.style.position = Position.Absolute;
            playheadLine.style.top = -2;
            playheadLine.style.bottom = -2;
            playheadLine.style.width = 2f;
            playheadLine.style.backgroundColor = colPlayhead;

            playheadCapTop = new VisualElement { pickingMode = PickingMode.Ignore };
            playheadCapTop.style.position = Position.Absolute;
            playheadCapTop.style.top = -4;
            playheadCapTop.style.left = -3;
            playheadCapTop.style.width = 8;
            playheadCapTop.style.height = 4;
            playheadCapTop.style.backgroundColor = colPlayhead;
            playheadLine.Add(playheadCapTop);

            playheadCapBottom = new VisualElement { pickingMode = PickingMode.Ignore };
            playheadCapBottom.style.position = Position.Absolute;
            playheadCapBottom.style.bottom = -4;
            playheadCapBottom.style.left = -3;
            playheadCapBottom.style.width = 8;
            playheadCapBottom.style.height = 4;
            playheadCapBottom.style.backgroundColor = colPlayhead;
            playheadLine.Add(playheadCapBottom);

            tracksArea.Add(playheadLine);

            lanesContainer.Add(tracksArea);
            UpdatePlayhead();
        }

        private VisualElement CreateLaneRow(string rowLabel, int[] mask, Color activeColor, DateTime t0Utc, int step_s, int n)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = RowHeight;
            row.style.marginBottom = RowGap;

            // Row Label
            var lbl = new Label(rowLabel);
            lbl.style.width = LabelWidth;
            lbl.style.fontSize = 11;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            lbl.style.color = new Color(0.68f, 0.76f, 0.86f);
            lbl.style.letterSpacing = 0.5f;
            row.Add(lbl);

            // Track Container
            var track = new VisualElement();
            track.style.flexGrow = 1;
            track.style.height = RowHeight;
            track.style.backgroundColor = colTrackBg;
            track.style.borderTopLeftRadius = 3;
            track.style.borderTopRightRadius = 3;
            track.style.borderBottomLeftRadius = 3;
            track.style.borderBottomRightRadius = 3;
            track.style.overflow = Overflow.Hidden;
            track.style.position = Position.Relative;

            if (mask != null && mask.Length > 0)
            {
                int cells = n - 1;
                var cellMask = new int[cells];
                Array.Copy(mask,cellMask,Math.Min(cells,mask.Length));
                var intervals = VisibilityMath.Windows(cellMask);
                foreach (var interval in intervals)
                {
                    float u0 = (float)interval.start / (n-1);
                    float u1 = (float)interval.endExcl / (n-1);
                    float uWidth = u1 - u0;

                    double segSec = (interval.endExcl - interval.start) * (double)step_s;
                    double segHours = segSec / 3600.0;
                    DateTime segStartUtc = t0Utc.AddSeconds(interval.start * step_s);
                    DateTime segEndUtc = t0Utc.AddSeconds(interval.endExcl * step_s);

                    var seg = new VisualElement();
                    seg.style.position = Position.Absolute;
                    seg.style.left = Length.Percent(u0 * 100f);
                    seg.style.width = Length.Percent(uWidth * 100f);
                    seg.style.height = Length.Percent(100f);
                    seg.style.backgroundColor = activeColor;
                    seg.style.justifyContent = Justify.Center;
                    seg.style.alignItems = Align.Center;

                    string tip = $"{rowLabel}: {segStartUtc:yyyy-MM-dd HH:mm} → {segEndUtc:yyyy-MM-dd HH:mm} UTC ({segHours:F1} h)";
                    seg.tooltip = tip;

                    // Display calculated duration INSIDE segments wide enough
                    // Threshold: >= 5% width (roughly 38px on a typical track)
                    if (uWidth >= 0.045f)
                    {
                        var segLabel = new Label($"{segHours:F1} h");
                        segLabel.style.fontSize = 11;
                        segLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                        segLabel.style.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);
                        segLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                        segLabel.pickingMode = PickingMode.Ignore;
                        seg.Add(segLabel);
                    }

                    // Hover feedback updates live bottom readout
                    seg.RegisterCallback<PointerEnterEvent>(evt => {
                        lblHoverStatus.text = "[SEGMENT] " + tip;
                    });
                    seg.RegisterCallback<PointerLeaveEvent>(evt => {
                        if (!isDraggingPlayhead) lblHoverStatus.text = DefaultHoverPrompt;
                    });

                    track.Add(seg);
                }
            }

            row.Add(track);
            return row;
        }

        private void UpdatePlayhead()
        {
            if (currentSite?.ephemeris == null || playheadLine == null) return;
            int n = currentSite.ephemeris.n;
            if (n <= 1) return;

            double totalSec = (n - 1) * (double)currentSite.ephemeris.step_s;
            float playheadU = Mathf.Clamp01((float)(currentTimeSec / totalSec));

            // Position playhead relative to track portion (starting at LabelWidth)
            float totalWidth = tracksArea.resolvedStyle.width;
            if (totalWidth > LabelWidth + 10f)
            {
                float trackW = totalWidth - LabelWidth;
                if (windowBracket != null)
                {
                    float startU=Mathf.Clamp01((float)(landingTimeSec/totalSec));
                    float endU=Mathf.Clamp01((float)((landingTimeSec+missionDurationSec)/totalSec));
                    windowBracket.style.left=LabelWidth+startU*trackW;
                    windowBracket.style.width=Mathf.Max(1,(endU-startU)*trackW);
                }
                float px = LabelWidth + playheadU * trackW;
                playheadLine.style.left = px;
            }
            else
            {
                // Fallback percentage
                playheadLine.style.left = Length.Percent(playheadU * 100f);
            }
        }

        private void OnTracksPointerDown(PointerDownEvent e)
        {
            if (currentSite?.ephemeris == null) return;
            isDraggingPlayhead = true;
            tracksArea.CapturePointer(e.pointerId);
            ScrubFromPointer(e.localPosition.x);
        }

        private void OnTracksPointerMove(PointerMoveEvent e)
        {
            if (currentSite?.ephemeris == null) return;

            if (isDraggingPlayhead)
            {
                ScrubFromPointer(e.localPosition.x);
            }
            else
            {
                // Live cursor timestamp readout
                float totalWidth = tracksArea.resolvedStyle.width;
                if (totalWidth > LabelWidth + 10f && e.localPosition.x >= LabelWidth)
                {
                    float trackW = totalWidth - LabelWidth;
                    float u = Mathf.Clamp01((e.localPosition.x - LabelWidth) / trackW);
                    int n = currentSite.ephemeris.n;
                    double totalSec = (n - 1) * (double)currentSite.ephemeris.step_s;
                    double curSec = u * totalSec;
                    double curH = curSec / 3600.0;

                    DateTime t0Utc = DateTime.UtcNow;
                    if (DateTime.TryParse(currentSite.ephemeris.t0_utc, CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
                    {
                        t0Utc = parsed;
                    }
                    DateTime curUtc = t0Utc.AddSeconds(curSec);
                    lblHoverStatus.text = $"[CURSOR] {curUtc:yyyy-MM-dd HH:mm:ss} UTC (T+{curH:F1} h)";
                }
            }
        }

        private void OnTracksPointerUp(PointerUpEvent e)
        {
            if (isDraggingPlayhead)
            {
                isDraggingPlayhead = false;
                tracksArea.ReleasePointer(e.pointerId);
                lblHoverStatus.text = DefaultHoverPrompt;
            }
        }

        private void ScrubFromPointer(float localX)
        {
            if (currentSite?.ephemeris == null) return;
            float totalWidth = tracksArea.resolvedStyle.width;
            if (totalWidth <= LabelWidth + 10f) return;

            float trackW = totalWidth - LabelWidth;
            float u = Mathf.Clamp01((localX - LabelWidth) / trackW);

            double totalSec = (currentSite.ephemeris.n - 1) * (double)currentSite.ephemeris.step_s;
            double sec = u * totalSec;
            currentTimeSec = sec;
            UpdatePlayhead();
            OnPlayheadDragged?.Invoke(sec);
        }
    }
}
