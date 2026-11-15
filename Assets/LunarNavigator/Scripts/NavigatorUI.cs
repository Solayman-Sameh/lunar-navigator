using System;
using System.Collections.Generic;
using System.Globalization;
using LunarNavigator.Data;
using LunarNavigator.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace LunarNavigator
{
    public enum AppMode { Inspect, Find }

    [RequireComponent(typeof(UIDocument))]
    public class NavigatorUI : MonoBehaviour
    {
        public StyleSheet styleSheet;
        public bool InterfaceVisible { get; private set; } = true;
        public AppMode Mode { get; private set; } = AppMode.Inspect;
        public string CurrentPopup { get; private set; } = "";
        public int VisibleMarkerCount { get; private set; }

        LunarApplication app;
        VisualElement root, overlay, headerBar, brandBlock, modeSwitchContainer, navPills;
        VisualElement statusBadge, floatingInspector, floatingMetrics, floatingFinder, timelineCard, surfaceOverlay;
        VisualElement popupModal, markerLayer;

        // Status Badge
        Label statusBadgeText;

        // Top bar buttons
        Button btnModeInspect, btnModeFind;
        readonly Dictionary<string, Button> navButtons = new Dictionary<string, Button>();

        // Inspector controls
        DropdownField siteDropdown;
        MissionConfigView missionConfigView;
        Button btnFlyToSite, btnAddCompare;
        int selectedDurationHours = 72;
        DateTime selectedLandingUtc = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        public TextField txtLandingUtc => missionConfigView?.TxtLandingUtc;
        public Button btnAnalyze => missionConfigView?.BtnAnalyze;

        // Mission Requirements
        Slider sliderMinSun, sliderMinEarth, sliderMaxDark, sliderMaxComm;
        VisualElement foldoutRequirements;

        // Metrics elements
        Label lblMetricSun, lblMetricEarth, lblMetricDark, lblMetricComm, lblMetricBoth, lblMetricsCaption;
        Label lblStatusSun, lblStatusEarth, lblStatusDark, lblStatusComm;

        // Custom Visual Elements
        TimelineElement timelineElement;
        SkylineChartElement skylineChartElement;

        // Timeline controls
        Button btnPlayPause, btnTimelineToggle;
        Label lblTimelineUtc, lblTimelineLossStatus;
        VisualElement timelineBody;

        // Opportunity Finder controls
        OpportunityFinderView opportunityFinderView;
        Button btnPolicyBest, btnPolicyEarliest;
        VisualElement oppResultsContainer;
        bool policyBestOverall = true;

        // Surface Overlay elements
        Label lblSurfaceCompass, lblSurfaceSunReadout, lblSurfaceEarthReadout;

        // 3D Pins
        readonly List<Button> markers = new List<Button>();
        readonly List<VisualElement> markerDots = new List<VisualElement>();
        readonly List<VisualElement> markerLines = new List<VisualElement>();

        // Comparison state
        bool hasComparison = false;
        DateTime compareLandingUtc;
        Metrics compareMetrics;
        Label lblCompareSummary;
        bool isInitializing = true;

        public void Initialize(LunarApplication value)
        {
            isInitializing = true;
            app = value;
            root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            root.pickingMode = PickingMode.Ignore;
            root.style.height = Length.Percent(100);
            root.style.width = Length.Percent(100);

            if (styleSheet) root.styleSheets.Add(styleSheet);

            overlay = new VisualElement { name = "overlay", pickingMode = PickingMode.Ignore };
            overlay.AddToClassList("overlay");
            root.Add(overlay);

            markerLayer = new VisualElement { name = "markers", pickingMode = PickingMode.Ignore };
            overlay.Add(markerLayer);

            BuildHeader();
            BuildInspectorCard();
            BuildMetricsCard();
            BuildFinderCard();
            BuildTimelineBar();
            BuildSurfaceOverlay();
            BuildPopupModal();
            BuildMarkers();

            // Set initial state from analysis repository
            if (AnalysisRepository.Instance.HasData)
            {
                selectedLandingUtc = AnalysisRepository.Instance.T0;
                if (missionConfigView != null) missionConfigView.SetLandingUtc(selectedLandingUtc);
            }

            isInitializing = false;
            SetMode(AppMode.Inspect);
            UpdateStatusBadge();
            RunAnalysis();

            root.RegisterCallback<PointerDownEvent>(e =>
            {
                if (CurrentPopup != "" && e.target == root) Close();
            });
        }

        private void BuildHeader()
        {
            headerBar = new VisualElement { name = "header-bar", pickingMode = PickingMode.Ignore };
            headerBar.AddToClassList("header-bar");
            overlay.Add(headerBar);

            // Left brand
            brandBlock = new VisualElement { name = "brand-block", pickingMode = PickingMode.Ignore };
            brandBlock.AddToClassList("brand-block");
            headerBar.Add(brandBlock);

            var brandRow = new VisualElement { name = "brand-row", pickingMode = PickingMode.Ignore };
            brandRow.AddToClassList("brand-row");
            brandBlock.Add(brandRow);

            var title = new Label("LUNAR NAVIGATOR");
            title.AddToClassList("brand-title");
            brandRow.Add(title);

            // Scientific Status Badge (ALWAYS VISIBLE)
            statusBadge = new VisualElement { name = "status-badge", pickingMode = PickingMode.Position };
            statusBadge.AddToClassList("status-badge");
            statusBadgeText = new Label("●  ILLUSTRATIVE DEMO");
            statusBadgeText.AddToClassList("status-badge-text");
            statusBadge.Add(statusBadgeText);
            brandRow.Add(statusBadge);

            var sub = new Label("SOUTH-POLE LANDING SITE EVALUATION");
            sub.AddToClassList("eyebrow");
            brandBlock.Add(sub);

            // Center: Mode Switch
            modeSwitchContainer = new VisualElement { name = "mode-switch", pickingMode = PickingMode.Ignore };
            modeSwitchContainer.AddToClassList("mode-switch-container");
            headerBar.Add(modeSwitchContainer);

            btnModeInspect = new Button(() => SetMode(AppMode.Inspect)) { text = "INSPECT SITE" };
            btnModeInspect.AddToClassList("mode-pill");
            modeSwitchContainer.Add(btnModeInspect);

            btnModeFind = new Button(() => SetMode(AppMode.Find)) { text = "FIND OPPORTUNITIES" };
            btnModeFind.AddToClassList("mode-pill");
            modeSwitchContainer.Add(btnModeFind);

            // Right: Nav pills
            navPills = new VisualElement { name = "nav-pills", pickingMode = PickingMode.Ignore };
            navPills.AddToClassList("nav-pills");
            headerBar.Add(navPills);

            foreach (var label in new[] { "Sites", "Layers", "Settings", "Help" })
            {
                var pill = new Button(() => Open(label)) { text = label };
                pill.AddToClassList("nav-pill");
                navPills.Add(pill);
                navButtons[label] = pill;
            }
        }

        private void BuildInspectorCard()
        {
            floatingInspector = new VisualElement { name = "floating-inspector", pickingMode = PickingMode.Position };
            floatingInspector.AddToClassList("floating-card");
            floatingInspector.AddToClassList("floating-card-left");
            overlay.Add(floatingInspector);

            var head = new VisualElement();
            head.AddToClassList("card-header");
            var title = new Label("SITE INSPECTOR");
            title.AddToClassList("card-title");
            head.Add(title);

            var closeBtn = new Button(() => floatingInspector.style.display = DisplayStyle.None) { text = "×" };
            closeBtn.AddToClassList("icon-btn");
            head.Add(closeBtn);
            floatingInspector.Add(head);

            // Site selector
            var siteChoices = new List<string>();
            foreach (var s in app.sites) siteChoices.Add(s.displayName);
            siteDropdown = new DropdownField("Landing Site", siteChoices, 0);
            siteDropdown.RegisterValueChangedCallback(e =>
            {
                int idx = siteChoices.IndexOf(e.newValue);
                if (idx >= 0 && idx < app.sites.Length)
                {
                    app.SelectSite(app.sites[idx]);
                    RunAnalysis();
                }
            });
            floatingInspector.Add(siteDropdown);

            btnFlyToSite = new Button(() => app.Director.ChangeMode(CameraMode.Approach)) { text = "Cinematic Approach  ↗" };
            btnFlyToSite.AddToClassList("btn-secondary");
            floatingInspector.Add(btnFlyToSite);

            // Mission Configuration (Editable UTC landing time, duration in hours, online API client)
            missionConfigView = new MissionConfigView();
            missionConfigView.SetLandingUtc(selectedLandingUtc);
            missionConfigView.SetDuration(selectedDurationHours);
            missionConfigView.OnConfigChanged += (utc, durationH) =>
            {
                selectedLandingUtc = utc;
                selectedDurationHours = (int)Math.Round(durationH);
                RunAnalysis();
            };
            missionConfigView.OnAnalyzeRequested += (utc, durationH) =>
            {
                selectedLandingUtc = utc;
                selectedDurationHours = (int)Math.Round(durationH);
                RequestOnlineAnalysis(utc, durationH);
            };
            missionConfigView.OnCancelRequested += () =>
            {
                LunarApiClient.Instance.CancelCurrentRequest();
            };
            floatingInspector.Add(missionConfigView);

            // Mission Requirements Foldout
            var reqFoldout = new Foldout { text = "Mission Constraints (Optional)", value = false };
            reqFoldout.style.marginTop = 6;
            sliderMinSun = new Slider("Min Sunlit %", 0, 100) { value = 30 }; reqFoldout.Add(sliderMinSun);
            sliderMinEarth = new Slider("Min Earth %", 0, 100) { value = 40 }; reqFoldout.Add(sliderMinEarth);
            sliderMaxDark = new Slider("Max Dark (h)", 0, 72) { value = 36 }; reqFoldout.Add(sliderMaxDark);
            sliderMaxComm = new Slider("Max Comm Gap (h)", 0, 72) { value = 48 }; reqFoldout.Add(sliderMaxComm);
            var note = new Label("Illustrative parameters. Not NASA standards.");
            note.AddToClassList("subtle");
            reqFoldout.Add(note);
            floatingInspector.Add(reqFoldout);

            btnAddCompare = new Button(ToggleComparison) { text = "+ Add Comparison" };
            btnAddCompare.AddToClassList("btn-secondary");
            floatingInspector.Add(btnAddCompare);

            lblCompareSummary = new Label("");
            lblCompareSummary.AddToClassList("subtle");
            lblCompareSummary.style.marginTop = 6;
            floatingInspector.Add(lblCompareSummary);
        }

        private void BuildMetricsCard()
        {
            floatingMetrics = new VisualElement { name = "floating-metrics", pickingMode = PickingMode.Position };
            floatingMetrics.AddToClassList("floating-card");
            floatingMetrics.AddToClassList("floating-card-right");
            overlay.Add(floatingMetrics);

            var head = new VisualElement();
            head.AddToClassList("card-header");
            var title = new Label("LINE-OF-SIGHT METRICS & SKYLINE");
            title.AddToClassList("card-title");
            head.Add(title);

            var closeBtn = new Button(() => floatingMetrics.AddToClassList("hidden")) { text = "×" };
            closeBtn.AddToClassList("icon-btn");
            head.Add(closeBtn);
            floatingMetrics.Add(head);

            // 2x2 Grid
            var grid = new VisualElement();
            grid.AddToClassList("metrics-grid");

            // Cell 1: Sunlit
            var c1 = new VisualElement(); c1.AddToClassList("metric-cell");
            var l1 = new Label("SUNLIT FRACTION"); l1.AddToClassList("metric-cell-label"); c1.Add(l1);
            lblMetricSun = new Label("--%"); lblMetricSun.AddToClassList("metric-cell-value"); c1.Add(lblMetricSun);
            lblStatusSun = new Label("PASS (min 30%)"); lblStatusSun.AddToClassList("metric-cell-status"); c1.Add(lblStatusSun);
            grid.Add(c1);

            // Cell 2: Earth Visible
            var c2 = new VisualElement(); c2.AddToClassList("metric-cell");
            var l2 = new Label("EARTH VISIBLE"); l2.AddToClassList("metric-cell-label"); c2.Add(l2);
            lblMetricEarth = new Label("--%"); lblMetricEarth.AddToClassList("metric-cell-value"); c2.Add(lblMetricEarth);
            lblStatusEarth = new Label("PASS (min 40%)"); lblStatusEarth.AddToClassList("metric-cell-status"); c2.Add(lblStatusEarth);
            grid.Add(c2);

            // Cell 3: Longest Darkness
            var c3 = new VisualElement(); c3.AddToClassList("metric-cell");
            var l3 = new Label("LONGEST DARKNESS"); l3.AddToClassList("metric-cell-label"); c3.Add(l3);
            lblMetricDark = new Label("-- h"); lblMetricDark.AddToClassList("metric-cell-value"); c3.Add(lblMetricDark);
            lblStatusDark = new Label("PASS (max 36h)"); lblStatusDark.AddToClassList("metric-cell-status"); c3.Add(lblStatusDark);
            grid.Add(c3);

            // Cell 4: Comms Gap
            var c4 = new VisualElement(); c4.AddToClassList("metric-cell");
            var l4 = new Label("MAX COMMS GAP"); l4.AddToClassList("metric-cell-label"); c4.Add(l4);
            lblMetricComm = new Label("-- h"); lblMetricComm.AddToClassList("metric-cell-value"); c4.Add(lblMetricComm);
            lblStatusComm = new Label("PASS (max 48h)"); lblStatusComm.AddToClassList("metric-cell-status"); c4.Add(lblStatusComm);
            grid.Add(c4);

            floatingMetrics.Add(grid);

            lblMetricBoth = new Label("Sun and Earth both visible: --%");
            lblMetricBoth.AddToClassList("metric-joint-line");
            floatingMetrics.Add(lblMetricBoth);

            lblMetricsCaption = new Label("Geometric line-of-sight only. Not a power or link-budget analysis.");
            lblMetricsCaption.AddToClassList("metric-caption");
            floatingMetrics.Add(lblMetricsCaption);

            // Skyline Chart
            skylineChartElement = new SkylineChartElement();
            floatingMetrics.Add(skylineChartElement);
        }

        private void BuildFinderCard()
        {
            floatingFinder = new VisualElement { name = "floating-finder", pickingMode = PickingMode.Position };
            floatingFinder.AddToClassList("floating-card");
            floatingFinder.AddToClassList("floating-card-left");
            floatingFinder.style.width = 380;
            floatingFinder.style.maxHeight = 680;
            floatingFinder.style.display = DisplayStyle.None;
            overlay.Add(floatingFinder);

            opportunityFinderView = new OpportunityFinderView();
            opportunityFinderView.OnCloseRequested += () => SetMode(AppMode.Inspect);
            opportunityFinderView.OnSearchRequested += RequestOnlineOpportunities;
            opportunityFinderView.OnCancelRequested += CancelOnlineOpportunities;
            opportunityFinderView.OnRecommendationSelected += SelectLiveOpportunity;
            floatingFinder.Add(opportunityFinderView);

            // Legacy backward-compatibility references
            oppResultsContainer = opportunityFinderView.ResultsScrollView;
            btnPolicyBest = opportunityFinderView.BtnPolicyBest;
            btnPolicyEarliest = opportunityFinderView.BtnPolicyEarliest;
        }

        private void BuildTimelineBar()
        {
            timelineCard = new VisualElement { name = "timeline-card", pickingMode = PickingMode.Position };
            timelineCard.AddToClassList("timeline-card");
            timelineCard.style.bottom = 20;
            timelineCard.style.width = 880;
            timelineCard.style.maxWidth = Length.Percent(95);
            overlay.Add(timelineCard);

            var head = new VisualElement();
            head.AddToClassList("timeline-header-row");

            var leftControls = new VisualElement();
            leftControls.AddToClassList("row");
            btnTimelineToggle = new Button(ToggleTimelineCollapse) { text = "TIMELINE ◇" };
            btnTimelineToggle.AddToClassList("chip-btn");
            leftControls.Add(btnTimelineToggle);

            btnPlayPause = new Button(() => app.Clock.Toggle()) { text = "▶" };
            btnPlayPause.AddToClassList("chip-btn");
            leftControls.Add(btnPlayPause);

            var bMinus1h = new Button(() => app.Clock.SetTime(app.Clock.seconds - 3600)) { text = "-1h" };
            bMinus1h.AddToClassList("chip-btn");
            leftControls.Add(bMinus1h);

            var bPlus1h = new Button(() => app.Clock.SetTime(app.Clock.seconds + 3600)) { text = "+1h" };
            bPlus1h.AddToClassList("chip-btn");
            leftControls.Add(bPlus1h);

            lblTimelineUtc = new Label("2026-12-01 00:00:00 UTC");
            lblTimelineUtc.AddToClassList("time-clock");
            lblTimelineUtc.style.marginLeft = 10;
            leftControls.Add(lblTimelineUtc);
            head.Add(leftControls);

            lblTimelineLossStatus = new Label("Sun visible");
            lblTimelineLossStatus.AddToClassList("time-loss-status");
            head.Add(lblTimelineLossStatus);

            timelineCard.Add(head);

            timelineBody = new VisualElement { name = "timeline-body" };
            timelineElement = new TimelineElement();
            timelineElement.OnPlayheadDragged += sec => app.Clock.SetTime(sec);
            timelineBody.Add(timelineElement);
            timelineCard.Add(timelineBody);
        }

        private void BuildSurfaceOverlay()
        {
            surfaceOverlay = new VisualElement { name = "surface-overlay", pickingMode = PickingMode.Ignore };
            surfaceOverlay.AddToClassList("surface-overlay");
            overlay.Add(surfaceOverlay);

            var leftBox = new VisualElement();
            leftBox.AddToClassList("surface-readout");
            lblSurfaceCompass = new Label("HEADING: 045° (NE)");
            lblSurfaceCompass.AddToClassList("surface-heading-ticks");
            leftBox.Add(lblSurfaceCompass);

            lblSurfaceSunReadout = new Label("Sun: --° above skyline");
            lblSurfaceSunReadout.AddToClassList("surface-body-status");
            leftBox.Add(lblSurfaceSunReadout);

            lblSurfaceEarthReadout = new Label("Earth: --° above skyline");
            lblSurfaceEarthReadout.AddToClassList("surface-body-status");
            leftBox.Add(lblSurfaceEarthReadout);
            surfaceOverlay.Add(leftBox);

            var rightBox = new VisualElement();
            rightBox.AddToClassList("surface-readout");
            var note = new Label("Skyline from site data. Foreground terrain: illustrative.");
            note.AddToClassList("subtle");
            rightBox.Add(note);
            surfaceOverlay.Add(rightBox);

            surfaceOverlay.style.display = DisplayStyle.None;
        }

        private void BuildPopupModal()
        {
            popupModal = new VisualElement { name = "popup-modal", pickingMode = PickingMode.Position };
            popupModal.AddToClassList("floating-card");
            popupModal.style.right = 28;
            popupModal.style.top = 96;
            popupModal.style.width = 340;
            popupModal.style.display = DisplayStyle.None;
            overlay.Add(popupModal);
        }

        private void BuildMarkers()
        {
            for (int i = 0; i < app.sites.Length; i++)
            {
                var site = app.sites[i];
                var dot = new VisualElement(); dot.AddToClassList("marker-point"); dot.pickingMode = PickingMode.Ignore;
                markerLayer.Add(dot); markerDots.Add(dot);

                var line = new VisualElement(); line.AddToClassList("marker-stem"); line.pickingMode = PickingMode.Ignore;
                markerLayer.Add(line); markerLines.Add(line);

                var marker = new Button(() => { app.SelectSite(site); RunAnalysis(); }) { text = "◉  " + site.displayName };
                marker.AddToClassList("marker-pin");
                marker.RegisterCallback<ClickEvent>(e => { if (e.clickCount == 2) app.Director.Focus(); });
                markerLayer.Add(marker); markers.Add(marker);
            }
        }

        public void SetMode(AppMode newMode)
        {
            Mode = newMode;
            if (btnModeInspect != null) btnModeInspect.EnableInClassList("mode-pill-active", Mode == AppMode.Inspect);
            if (btnModeFind != null) btnModeFind.EnableInClassList("mode-pill-active", Mode == AppMode.Find);

            if (floatingInspector != null)
                floatingInspector.style.display = (InterfaceVisible && Mode == AppMode.Inspect) ? DisplayStyle.Flex : DisplayStyle.None;
            if (floatingMetrics != null)
                floatingMetrics.style.display = InterfaceVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (floatingFinder != null)
                floatingFinder.style.display = (InterfaceVisible && Mode == AppMode.Find) ? DisplayStyle.Flex : DisplayStyle.None;

            if (Mode == AppMode.Find)
            {
                RefreshOpportunitiesList();
            }
        }

        public void ToggleInterface()
        {
            InterfaceVisible = !InterfaceVisible;
            if (floatingInspector != null)
                floatingInspector.style.display = InterfaceVisible && Mode == AppMode.Inspect ? DisplayStyle.Flex : DisplayStyle.None;
            if (floatingMetrics != null)
                floatingMetrics.style.display = InterfaceVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (floatingFinder != null)
                floatingFinder.style.display = InterfaceVisible && Mode == AppMode.Find ? DisplayStyle.Flex : DisplayStyle.None;
            if (timelineCard != null)
                timelineCard.style.display = InterfaceVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (modeSwitchContainer != null)
                modeSwitchContainer.style.display = InterfaceVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (navPills != null)
                navPills.style.display = InterfaceVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (markerLayer != null)
                markerLayer.style.display = InterfaceVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (popupModal != null)
                popupModal.style.display = DisplayStyle.None;
        }

        public bool Close()
        {
            if (CurrentPopup != "")
            {
                CurrentPopup = "";
                popupModal.style.display = DisplayStyle.None;
                return true;
            }
            return false;
        }

        public void Open(string title)
        {
            if (CurrentPopup == title) { Close(); return; }
            CurrentPopup = title;
            popupModal.Clear();
            popupModal.style.display = DisplayStyle.Flex;

            var head = new VisualElement(); head.AddToClassList("card-header");
            head.Add(new Label(title.ToUpperInvariant()) { name = "modal-title" });
            head.Add(new Button(() => Close()) { text = "×" });
            popupModal.Add(head);

            if (title == "Sites")
            {
                var repo = AnalysisRepository.Instance;
                foreach (var site in app.sites)
                {
                    var s = site;
                    var b = new Button(() => { app.SelectSite(s); RunAnalysis(); Close(); })
                    {
                        text = $"{s.displayName}  ({s.latitude:F2}°, {s.longitude:F2}°)"
                    };
                    b.AddToClassList("btn-secondary");
                    popupModal.Add(b);
                }
            }
            else if (title == "Layers")
            {
                AddToggle("South Pole Site Pins", app.World.markersVisible, v => app.World.markersVisible = v);
                AddToggle("Cosmic Star Background", app.World.stars.activeSelf, v => app.World.stars.SetActive(v));
                AddToggle("Direction Vectors", app.World.aidsVisible, v => app.World.aidsVisible = v);
            }
            else if (title == "Settings")
            {
                var gLabel = new Label("GRAPHICS PRESET"); gLabel.AddToClassList("eyebrow"); popupModal.Add(gLabel);
                foreach (var p in new[] { "Performance", "Balanced", "Cinematic" })
                {
                    var btn = new Button(() => { app.World.ApplyQuality(p); Open("Settings"); }) { text = p };
                    btn.AddToClassList("chip-btn");
                    if (app.World.Preset == p) btn.AddToClassList("chip-btn-active");
                    popupModal.Add(btn);
                }
                var speedLabel = new Label("SIMULATION SPEED"); speedLabel.AddToClassList("eyebrow"); speedLabel.style.marginTop = 10; popupModal.Add(speedLabel);
                var sRow = new VisualElement(); sRow.AddToClassList("row");
                foreach (double sp in new double[] { 1, 60, 3600, 86400 })
                {
                    var b = new Button(() => app.Clock.speed = sp) { text = sp + "×" };
                    b.AddToClassList("chip-btn");
                    sRow.Add(b);
                }
                popupModal.Add(sRow);
            }
            else if (title == "Help")
            {
                var hText = new Label(
                    "KEYBOARD CONTROLS:\n" +
                    "1 · Global Orbit mode\n" +
                    "2 · Cinematic Approach descent\n" +
                    "3 · Local Orbit mode\n" +
                    "4 · Surface Observer look\n" +
                    "Space · Play / Pause time\n" +
                    "F · Focus selection | R · Reset view\n" +
                    "Tab · Fullscreen Immersion mode\n" +
                    "Esc · Dismiss popup / Return mode\n\n" +
                    "SCIENTIFIC NOTICE:\n" +
                    "Line-of-sight geometry only. Not a power, link-budget or landing-safety analysis. Visual celestial scale is illustrative."
                );
                hText.AddToClassList("subtle");
                popupModal.Add(hText);

                if (AnalysisRepository.Instance.DataProblems.Count > 0)
                {
                    var pLabel = new Label("DATA NOTICES:"); pLabel.AddToClassList("eyebrow"); pLabel.style.marginTop = 10; popupModal.Add(pLabel);
                    foreach (var prob in AnalysisRepository.Instance.DataProblems)
                    {
                        var pText = new Label("• " + prob); pText.AddToClassList("subtle"); popupModal.Add(pText);
                    }
                }
            }
            else if (title == "Simulation")
            {
                var speedLabel = new Label("SIMULATION CONTROLS"); speedLabel.AddToClassList("eyebrow"); popupModal.Add(speedLabel);
                var row = new VisualElement(); row.AddToClassList("row");
                var bPlay = new Button(() => app.Clock.paused = !app.Clock.paused) { text = app.Clock.paused ? "PLAY" : "PAUSE" };
                bPlay.AddToClassList("chip-btn");
                row.Add(bPlay);
                foreach (double sp in new double[] { 1, 60, 3600, 86400 })
                {
                    var b = new Button(() => app.Clock.speed = sp) { text = sp + "×" };
                    b.AddToClassList("chip-btn");
                    row.Add(b);
                }
                popupModal.Add(row);
                var info = new Label($"CURRENT UTC: {app.Clock.Utc:yyyy-MM-dd HH:mm:ss} UTC");
                info.AddToClassList("subtle");
                info.style.marginTop = 8;
                popupModal.Add(info);
            }
            else if (title == "Site information")
            {
                var s = app.SelectedSite;
                if (s != null)
                {
                    var nameLbl = new Label(s.displayName); nameLbl.AddToClassList("metric-value"); popupModal.Add(nameLbl);
                    var coords = new Label($"Coordinates: {s.latitude:F2}°, {s.longitude:F2}° (Elev: {s.elevation:F1}m)"); coords.AddToClassList("subtle"); popupModal.Add(coords);
                    var desc = new Label(s.description); desc.AddToClassList("subtle"); desc.style.marginTop = 6; popupModal.Add(desc);
                    var prov = new Label($"Provenance: {s.provenance}"); prov.AddToClassList("subtle"); prov.style.marginTop = 6; popupModal.Add(prov);
                }
            }
        }

        private void AddToggle(string label, bool val, Action<bool> onChange)
        {
            var t = new Toggle(label) { value = val };
            t.RegisterValueChangedCallback(e => onChange(e.newValue));
            t.style.marginTop = 6;
            popupModal.Add(t);
        }

        public void OnSiteChanged(LunarSite site)
        {
            if (siteDropdown != null && site != null)
            {
                siteDropdown.SetValueWithoutNotify(site.displayName);
            }
            RunAnalysis();
        }

        private void ValidateAndSetLandingTime(string text)
        {
            if (isInitializing) return;
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
            {
                if (txtLandingUtc != null) txtLandingUtc.style.borderBottomColor = new StyleColor(StyleKeyword.Null);
                selectedLandingUtc = dt;
                RunAnalysis();
            }
            else
            {
                if (txtLandingUtc != null) txtLandingUtc.style.borderBottomColor = new Color(0.9f, 0.2f, 0.2f, 1f);
                if (lblMetricBoth != null) lblMetricBoth.text = "Invalid date format. Expected: yyyy-MM-dd HH:mm";
            }
        }

        private void StepLandingTime(double deltaSec)
        {
            selectedLandingUtc = selectedLandingUtc.AddSeconds(deltaSec);
            if (missionConfigView != null) missionConfigView.SetLandingUtc(selectedLandingUtc);
            RunAnalysis();
        }

        private void SetDuration(int hours)
        {
            selectedDurationHours = hours;
            if (missionConfigView != null) missionConfigView.SetDuration(hours);
            RunAnalysis();
        }

        public void RunAnalysis()
        {
            if (isInitializing || lblMetricSun == null) return;
            var siteData = AnalysisRepository.Instance.GetSite(app?.SelectedSite?.id);
            if (siteData == null) return;

            DateTime t0 = AnalysisRepository.Instance.T0;
            double secFromT0 = (selectedLandingUtc.ToUniversalTime() - t0).TotalSeconds;
            double landingSec = Math.Max(0, secFromT0);

            try
            {
                Metrics m;
                if (siteData.metrics != null && Math.Abs((selectedLandingUtc.ToUniversalTime() - t0).TotalSeconds) < 60)
                {
                    m = new Metrics
                    {
                        start_utc = siteData.metrics.start_utc,
                        duration_h = (int)Math.Round(siteData.metrics.duration_h),
                        f_sun = siteData.metrics.f_sun,
                        f_earth = siteData.metrics.f_earth,
                        f_both = siteData.metrics.f_both,
                        dark_max_h = siteData.metrics.dark_max_h,
                        comm_gap_max_h = siteData.metrics.comm_gap_max_h
                    };
                }
                else
                {
                    m = VisibilityMath.ComputeForUtc(siteData, selectedLandingUtc, selectedDurationHours);
                }

                lblMetricSun.text = $"{m.f_sun * 100.0:F1}%";
                lblMetricEarth.text = $"{m.f_earth * 100.0:F1}%";
                lblMetricDark.text = $"{m.dark_max_h:F1} h";
                lblMetricComm.text = $"{m.comm_gap_max_h:F1} h";
                lblMetricBoth.text = $"Sun & Earth both visible: {m.f_both * 100.0:F1}%";

                if (siteData.HasIncompleteSectors)
                {
                    bool uncertified = false;
                    int sampleCount = (int)Math.Round(selectedDurationHours * 3600.0 / siteData.ephemeris.step_s);
                    int startIdx = (int)Math.Round((selectedLandingUtc.ToUniversalTime() - AnalysisRepository.Instance.T0).TotalSeconds / siteData.ephemeris.step_s);
                    startIdx = Mathf.Clamp(startIdx, 0, siteData.ephemeris.n - 1);
                    int endIdx = Mathf.Clamp(startIdx + sampleCount, 0, siteData.ephemeris.n);
                    for (int k = startIdx; k < endIdx; k++)
                    {
                        if (VisibilityMath.IsIncompleteSector(siteData, siteData.ephemeris.sun_az_deg[k]) ||
                            VisibilityMath.IsIncompleteSector(siteData, siteData.ephemeris.earth_az_deg[k]))
                        {
                            uncertified = true;
                            break;
                        }
                    }
                    if (uncertified)
                    {
                        lblMetricBoth.text += "  [⚠ UNCERTAIN: Incomplete DEM Sector]";
                    }
                }

                // Update scientific display safety caption
                if (lblMetricsCaption != null)
                {
                    if (siteData.data_status == "GEOMETRIC_ONLY")
                        lblMetricsCaption.text = "Geometric spherical horizon (-0.09° dip). Real LOLA DEM terrain horizon not yet certified.";
                    else if (siteData.data_status == "TERRAIN_AWARE_ESTIMATE")
                        lblMetricsCaption.text = "Terrain-aware LOLA DEM skyline. Not a power or link-budget analysis.";
                    else
                        lblMetricsCaption.text = "Illustrative DEM skyline demo. Not a power or link-budget analysis.";
                }

                // Pass/Fail check against requirements
                float minSun = sliderMinSun?.value ?? 30f;
                float minEarth = sliderMinEarth?.value ?? 40f;
                float maxDark = sliderMaxDark?.value ?? 36f;
                float maxComm = sliderMaxComm?.value ?? 48f;

                UpdateMetricStatus(lblStatusSun, (float)(m.f_sun * 100.0) >= minSun, $"Min {minSun:F0}%");
                UpdateMetricStatus(lblStatusEarth, (float)(m.f_earth * 100.0) >= minEarth, $"Min {minEarth:F0}%");
                UpdateMetricStatus(lblStatusDark, (float)m.dark_max_h <= maxDark, $"Max {maxDark:F0}h");
                UpdateMetricStatus(lblStatusComm, (float)m.comm_gap_max_h <= maxComm, $"Max {maxComm:F0}h");

                // Update Timeline and Skyline Chart
                timelineElement?.SetData(siteData, app.Clock.seconds, landingSec, selectedDurationHours);
                skylineChartElement?.SetData(siteData, app.Clock.seconds, landingSec, selectedDurationHours);

                if (hasComparison)
                {
                    var comp = VisibilityMath.ComputeForUtc(siteData, compareLandingUtc, selectedDurationHours);
                    float dSun = (float)((m.f_sun - comp.f_sun) * 100.0);
                    float dDark = (float)(m.dark_max_h - comp.dark_max_h);
                    lblCompareSummary.text = $"Comparison: Sunlit {(dSun >= 0 ? "+" : "")}{dSun:F1} pts | Max Dark {(dDark >= 0 ? "+" : "")}{dDark:F1} h";
                }
            }
            catch (Exception ex)
            {
                lblMetricSun.text = "N/A";
                lblMetricEarth.text = "N/A";
                lblMetricDark.text = "N/A";
                lblMetricComm.text = "N/A";
                if (lblMetricBoth != null) lblMetricBoth.text = "Window outside loaded coverage. Click ANALYZE to query NASA service.";
                Debug.LogWarning("[NavigatorUI] Analysis window out of range: " + ex.Message);
            }
        }

        private void UpdateMetricStatus(Label lbl, bool pass, string reqText)
        {
            if (lbl == null) return;
            lbl.text = (pass ? "PASS (" : "FAIL (") + reqText + ")";
            lbl.EnableInClassList("metric-cell-status-fail", !pass);
        }

        private void ToggleComparison()
        {
            hasComparison = !hasComparison;
            if (hasComparison)
            {
                compareLandingUtc = selectedLandingUtc.AddDays(2);
                btnAddCompare.text = "Remove Comparison";
            }
            else
            {
                btnAddCompare.text = "+ Add Comparison";
                lblCompareSummary.text = "";
            }
            RunAnalysis();
        }

        private void SetFinderPolicy(bool bestOverall)
        {
            policyBestOverall = bestOverall;
            if (opportunityFinderView != null)
            {
                opportunityFinderView.SetPolicy(policyBestOverall ? "BEST_OVERALL" : "EARLIEST_QUALIFYING");
            }
            RefreshOpportunitiesList();
        }

        private void RefreshOpportunitiesList()
        {
            if (opportunityFinderView != null && AnalysisRepository.Instance.LiveOpportunities != null)
            {
                opportunityFinderView.DisplayResults(AnalysisRepository.Instance.LiveOpportunities);
                return;
            }

            if (oppResultsContainer == null) return;
            oppResultsContainer.Clear();

            var opp = AnalysisRepository.Instance.Opportunities;
            if (opp == null)
            {
                oppResultsContainer.Add(new Label("No opportunities dataset found.") { name = "opp-empty" });
                return;
            }

            var list = policyBestOverall ? opp.best_overall : opp.earliest_qualifying;
            if (list == null || list.Length == 0)
            {
                oppResultsContainer.Add(new Label("No qualifying opportunities meet all constraints.") { name = "opp-none" });
                return;
            }

            foreach (var r in list)
            {
                var card = new VisualElement();
                card.AddToClassList("opp-card");

                var head = new VisualElement();
                head.AddToClassList("opp-head");
                var sLabel = new Label($"#{r.rank}  {FormatSiteName(r.site_id)}");
                sLabel.AddToClassList("opp-site");
                head.Add(sLabel);

                var scLabel = new Label($"Score: {r.score:F0}");
                scLabel.AddToClassList("opp-score");
                head.Add(scLabel);
                card.Add(head);

                var tLabel = new Label($"Landing: {r.landing_utc} ({r.duration_h}h)");
                tLabel.AddToClassList("opp-time");
                card.Add(tLabel);

                var mRow = new VisualElement();
                mRow.AddToClassList("opp-metrics-row");
                var lblSun = new Label($"Sun: {r.f_sun * 100.0:F0}%") { name = "m-sun" };
                lblSun.AddToClassList("opp-metric-item");
                mRow.Add(lblSun);
                var lblEarth = new Label($"Earth: {r.f_earth * 100.0:F0}%") { name = "m-earth" };
                lblEarth.AddToClassList("opp-metric-item");
                mRow.Add(lblEarth);
                var lblDark = new Label($"Dark: {r.dark_max_h:F0}h") { name = "m-dark" };
                lblDark.AddToClassList("opp-metric-item");
                mRow.Add(lblDark);
                card.Add(mRow);

                card.RegisterCallback<ClickEvent>(e => SelectOpportunity(r));
                oppResultsContainer.Add(card);
            }
        }

        public void RequestOnlineOpportunities(LiveOpportunityRequest req)
        {
            if (opportunityFinderView == null) return;
            opportunityFinderView.SetLoading(true, "Searching NASA Horizons via FastAPI backend...");

            string jsonPayload = req.ToJson();
            LunarApiClient.Instance.PostOpportunities(jsonPayload, (success, jsonResponse, errorMsg) =>
            {
                if (success)
                {
                    try
                    {
                        var resp = JsonUtility.FromJson<LiveOpportunitiesResponse>(jsonResponse);
                        if (resp != null)
                        {
                            AnalysisRepository.Instance.SetLiveOpportunities(resp);
                            opportunityFinderView.DisplayResults(resp);
                        }
                        else
                        {
                            opportunityFinderView.ShowError("Failed to deserialize opportunities response from scientific backend.");
                        }
                    }
                    catch (Exception ex)
                    {
                        opportunityFinderView.ShowError($"JSON Deserialization Exception: {ex.Message}");
                    }
                }
                else
                {
                    opportunityFinderView.ShowError(errorMsg);
                    // Do not silently substitute synthetic data; keep failure visible
                }
            });
        }

        public void CancelOnlineOpportunities()
        {
            if (LunarApiClient.Instance != null)
            {
                LunarApiClient.Instance.CancelCurrentRequest();
            }
            if (opportunityFinderView != null)
            {
                opportunityFinderView.SetLoading(false);
                opportunityFinderView.ShowError("Opportunity search canceled by user.");
            }
        }

        public void SelectLiveOpportunity(LiveOpportunityRecommendation opp)
        {
            if (opp == null) return;
            if (app == null) app = FindAnyObjectByType<LunarApplication>();

            var targetSite = (app != null && app.sites != null)
                ? Array.Find(app.sites, s => s.id == opp.site_id || s.displayName.Replace(" ", "_").Equals(opp.site_id, StringComparison.OrdinalIgnoreCase))
                : null;

            if (targetSite != null && app != null)
            {
                app.SelectSite(targetSite);
            }

            if (DateTime.TryParse(opp.landing_utc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
            {
                selectedLandingUtc = dt;
                if (missionConfigView != null) missionConfigView.SetLandingUtc(dt);
            }
            selectedDurationHours = (int)Math.Round(opp.mission_duration_h);
            if (missionConfigView != null) missionConfigView.SetDuration(opp.mission_duration_h);

            // Synchronize Site Inspector and celestial simulation with actual NASA data for this scenario
            RequestOnlineAnalysis(selectedLandingUtc, selectedDurationHours);

            if (app && app.Director)
            {
                if (targetSite != null) app.Director.Select(targetSite);
                app.Director.Focus();
            }
        }

        private string FormatSiteName(string id)
        {
            if (string.IsNullOrEmpty(id)) return "Unknown";
            string[] parts = id.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            }
            return string.Join(" ", parts);
        }

        private void SelectOpportunity(OpportunityResult opp)
        {
            var targetSite = Array.Find(app.sites, s => s.id == opp.site_id || s.displayName.Replace(" ", "_").Equals(opp.site_id, StringComparison.OrdinalIgnoreCase));
            if (targetSite != null)
            {
                app.SelectSite(targetSite);
            }

            if (DateTime.TryParse(opp.landing_utc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
            {
                selectedLandingUtc = dt;
                if (missionConfigView != null) missionConfigView.SetLandingUtc(dt);
            }
            selectedDurationHours = opp.duration_h;
            if (missionConfigView != null) missionConfigView.SetDuration(opp.duration_h);

            RunAnalysis();
            app.Director.ChangeMode(CameraMode.Approach);
        }

        private void RequestOnlineAnalysis(DateTime landingUtc, double durationH)
        {
            if (missionConfigView == null) return;
            missionConfigView.SetLoading(true, "Connecting to NASA Horizons via FastAPI backend...");

            var selectedSite = app?.SelectedSite;
            var payload = new AnalyzeRequestPayload
            {
                site_id = selectedSite?.id,
                latitude_deg = selectedSite != null ? (double)selectedSite.latitude : -89.5,
                longitude_deg = selectedSite != null ? (double)selectedSite.longitude : 0.0,
                landing_utc = landingUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                mission_duration_h = durationH,
                observer_height_m = 2.0,
                sampling_step_minutes = 10
            };

            LunarApiClient.Instance.PostAnalyze(payload, (success, jsonResponse, errorMsg) =>
            {
                if (success)
                {
                    missionConfigView.SetLoading(false);
                    missionConfigView.ClearError();

                    var adapted = ScienceApiAdapter.AdaptApiResponse(
                        jsonResponse,
                        selectedSite?.id,
                        selectedSite?.displayName,
                        out string adaptError);

                    if (adapted != null)
                    {
                        AnalysisRepository.Instance.SetDynamicAnalysis(adapted);
                        if (app && app.Clock)
                        {
                            app.Clock.SetTime(0);
                        }
                        RunAnalysis();
                        UpdateStatusBadge();
                        if (app && app.World)
                        {
                            app.World.UpdateSkyline();
                        }
                    }
                    else
                    {
                        missionConfigView.ShowError($"Scientific Data Adaptation Failed: {adaptError}");
                    }
                }
                else
                {
                    missionConfigView.SetLoading(false);
                    missionConfigView.ShowError(errorMsg);
                    // Do not silently substitute illustrative data; keep failure visible
                }
            });
        }

        private void ToggleTimelineCollapse()
        {
            if (timelineBody == null) return;
            bool isCollapsed = timelineBody.style.display == DisplayStyle.None;
            timelineBody.style.display = isCollapsed ? DisplayStyle.Flex : DisplayStyle.None;
            btnTimelineToggle.text = isCollapsed ? "TIMELINE ◇" : "TIMELINE ▾";
        }

        private void UpdateStatusBadge()
        {
            if (statusBadgeText == null) return;
            string status = AnalysisRepository.Instance.DataStatus;

            statusBadge.RemoveFromClassList("status-badge-illustrative");
            statusBadge.RemoveFromClassList("status-badge-geometric");
            statusBadge.RemoveFromClassList("status-badge-terrain");

            if (status == "INVALID_DATA" || AnalysisRepository.Instance.Sites.Count == 0)
            {
                statusBadgeText.text = "●  DATA REJECTED";
                statusBadge.AddToClassList("status-badge-illustrative");
                statusBadge.tooltip = "Scientific data failed contract validation.";
                return;
            }

            if (status == "TERRAIN_AWARE_ESTIMATE")
            {
                statusBadgeText.text = "●  TERRAIN-AWARE ESTIMATE";
                statusBadge.AddToClassList("status-badge-terrain");
                statusBadge.tooltip = "Real JPL Horizons ephemeris + LOLA DEM terrain horizon profile.";
            }
            else if (status == "GEOMETRIC_ONLY")
            {
                statusBadgeText.text = "●  GEOMETRIC HORIZON ONLY";
                statusBadge.AddToClassList("status-badge-geometric");
                statusBadge.tooltip = "Real JPL Horizons ephemeris + geometric spherical horizon.";
            }
            else
            {
                statusBadgeText.text = "●  ILLUSTRATIVE DEMO";
                statusBadge.AddToClassList("status-badge-illustrative");
                statusBadge.tooltip = "Synthetic demonstrative tracks and harmonic terrain.";
            }
        }

        public bool IsPointerOverUI(Vector2 screen)
        {
            if (!InterfaceVisible) return false;
            var panel = root?.panel;
            if (panel == null) return false;

            Vector2 p = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, Screen.height - screen.y));
            var picked = panel.Pick(p);
            if (picked == null) return false;

            return picked != root && picked != overlay && picked != markerLayer && picked != headerBar && picked != modeSwitchContainer;
        }

        public void PickMarker(Vector2 screen, bool focus)
        {
            if (CurrentPopup != "") { Close(); return; }
            float best = 35f;
            LunarSite chosen = null;

            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i].style.display == DisplayStyle.None) continue;
                var sp = app.Director.view.WorldToScreenPoint(app.sites[i].Direction * (app.config.radius + 3));
                float d = Vector2.Distance(new Vector2(sp.x, sp.y), screen);
                if (d < best) { best = d; chosen = app.sites[i]; }
            }

            if (chosen)
            {
                app.SelectSite(chosen);
                RunAnalysis();
                if (focus) app.Director.Focus();
            }
        }

        void Update()
        {
            if (!app || root == null) return;

            // Clock update
            lblTimelineUtc.text = app.Clock.Utc.ToString("yyyy-MM-dd  HH:mm:ss 'UTC'").ToUpperInvariant();
            btnPlayPause.text = app.Clock.paused ? "▶" : "Ⅱ";

            timelineElement?.SetCurrentTime(app.Clock.seconds);
            skylineChartElement?.SetCurrentTime(app.Clock.seconds);

            // Compute next loss of sun
            var siteData = AnalysisRepository.Instance.GetSite(app?.SelectedSite?.id);
            if (siteData?.visibility?.sun_visible != null && siteData.ephemeris != null)
            {
                int curIdx = Mathf.Clamp((int)Math.Floor(app.Clock.seconds / siteData.ephemeris.step_s), 0, siteData.ephemeris.n - 1);
                int lossSamples = VisibilityMath.NextLoss(siteData.visibility.sun_visible, curIdx);
                if (lossSamples == 0)
                {
                    lblTimelineLossStatus.text = "Sun currently occulted by terrain";
                }
                else if (lossSamples > 0)
                {
                    float lossH = lossSamples * (siteData.ephemeris.step_s / 3600f);
                    lblTimelineLossStatus.text = $"Next Sun loss in {lossH:F1} h";
                }
                else
                {
                    lblTimelineLossStatus.text = "Sun visible throughout dataset";
                }
            }

            // Surface mode overlay
            bool isSurface = app.Director.Mode == CameraMode.Surface;
            surfaceOverlay.style.display = isSurface ? DisplayStyle.Flex : DisplayStyle.None;
            if (isSurface && siteData?.ephemeris != null)
            {
                var dir = app.Director;
                float heading = Mathf.Repeat(dir.surfaceYaw, 360f);
                lblSurfaceCompass.text = $"HORIZON HEADING: {heading:F0}° ({GetCompassName(heading)})";

                var frame = Quaternion.Inverse(dir.Frame);
                var sd = frame * app.Clock.Provider.SunDirection(app.Clock.Utc);
                var ed = frame * app.Clock.Provider.EarthDirection(app.Clock.Utc);

                float sunAz = Mathf.Repeat(Mathf.Atan2(sd.x, sd.z) * Mathf.Rad2Deg, 360f);
                float sunEl = Mathf.Asin(Mathf.Clamp(sd.y, -1f, 1f)) * Mathf.Rad2Deg;
                float sunSky = VisibilityMath.SkylineAt(siteData.terrain.horizon_el_deg, sunAz);
                float sunDiff = sunEl - sunSky;

                lblSurfaceSunReadout.text = sunDiff >= 0
                    ? $"Sun: {sunDiff:F1}° above skyline (Az {sunAz:F0}°, El {sunEl:F1}°)"
                    : $"Sun: {-sunDiff:F1}° below skyline (OCCULTED)";

                float earthAz = Mathf.Repeat(Mathf.Atan2(ed.x, ed.z) * Mathf.Rad2Deg, 360f);
                float earthEl = Mathf.Asin(Mathf.Clamp(ed.y, -1f, 1f)) * Mathf.Rad2Deg;
                float earthSky = VisibilityMath.SkylineAt(siteData.terrain.horizon_el_deg, earthAz);
                float earthDiff = earthEl - earthSky;

                lblSurfaceEarthReadout.text = earthDiff >= 0
                    ? $"Earth: {earthDiff:F1}° above skyline (Az {earthAz:F0}°, El {earthEl:F1}°)"
                    : $"Earth: {-earthDiff:F1}° below skyline (OCCULTED)";
            }

            // 3D Site Pins projection
            VisibleMarkerCount = 0;
            var director = app.Director;
            for (int i = 0; i < markers.Count; i++)
            {
                var site = app.sites[i];
                var point = site.Direction * (app.config.radius + 3);
                var sp = director.view.WorldToScreenPoint(point);
                bool visible = InterfaceVisible && app.World.markersVisible && director.Mode == CameraMode.Global && sp.z > 0 && Vector3.Dot(site.Direction, director.view.transform.position - point) > 0;

                markers[i].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                markerDots[i].style.display = markerLines[i].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

                if (visible)
                {
                    VisibleMarkerCount++;
                    Vector2 panel = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(sp.x, Screen.height - sp.y));
                    markers[i].style.left = Mathf.Clamp(panel.x + 14, 20, root.resolvedStyle.width - 165);
                    markers[i].style.top = Mathf.Clamp(panel.y - 100, 140, root.resolvedStyle.height - 350) + i * 28;
                    markerDots[i].style.left = panel.x - 3;
                    markerDots[i].style.top = panel.y - 3;

                    var end = new Vector2(markers[i].style.left.value.value, markers[i].style.top.value.value + 11);
                    var delta = end - panel;
                    markerLines[i].style.left = panel.x;
                    markerLines[i].style.top = panel.y;
                    markerLines[i].style.width = delta.magnitude;
                    markerLines[i].style.rotate = new Rotate(Angle.Degrees(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg));

                    markers[i].EnableInClassList("marker-pin-selected", site == app.SelectedSite);
                }
            }
        }

        private string GetCompassName(float deg)
        {
            if (deg >= 337.5f || deg < 22.5f) return "N";
            if (deg >= 22.5f && deg < 67.5f) return "NE";
            if (deg >= 67.5f && deg < 112.5f) return "E";
            if (deg >= 112.5f && deg < 157.5f) return "SE";
            if (deg >= 157.5f && deg < 202.5f) return "S";
            if (deg >= 202.5f && deg < 247.5f) return "SW";
            if (deg >= 247.5f && deg < 292.5f) return "W";
            return "NW";
        }
    }
}
