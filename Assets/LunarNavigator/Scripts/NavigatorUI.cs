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
        Button btnReopenInspector, btnReopenMetrics;
        bool inspectorVisible = true, metricsVisible = true;

        // Status Badge
        Label statusBadgeText;
        Label selectionLabel, analysisFeedback, resultsInterpretation, guidanceLabel;
        ScrollView popupBody;
        VisualElement popupScrim;
        PanelSettings originalPanel, runtimePanel;
        float ignoreWorldPickUntil;

        // Top bar buttons
        Button btnModeInspect, btnModeFind;
        readonly Dictionary<string, Button> navButtons = new Dictionary<string, Button>();

        // Inspector controls
        DropdownField siteDropdown;
        MissionConfigView missionConfigView;
        Button btnAddCompare;
        double selectedDurationHours = 72;
        DateTime selectedLandingUtc = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        public TextField txtLandingUtc => missionConfigView?.TxtLandingUtc;
        public Button btnAnalyze => missionConfigView?.BtnAnalyze;

        // Mission Requirements
        Slider sliderMinSun, sliderMinEarth, sliderMaxDark, sliderMaxComm;
        VisualElement foldoutRequirements;

        // Metrics elements
        Label lblMetricSun, lblMetricEarth, lblMetricDark, lblMetricComm, lblMetricBoth, lblMetricsCaption;
        Label lblStatusSun, lblStatusEarth, lblStatusDark, lblStatusComm;
        MissionAssessmentView assessmentView;
        SiteAnalysis assessedData;
        string assessedSiteId, assessmentState = "Awaiting analysis", assessmentMessage = "Select a location and analyze your mission window.";
        DateTime assessedLandingUtc;
        double assessedDurationHours;

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
        Button earthMarker;
        VisualElement earthMarkerDot;
        Button sunMarker;
        VisualElement sunMarkerDot;

        // Comparison state
        bool hasComparison = false;
        DateTime compareLandingUtc;
        Metrics compareMetrics;
        Label lblCompareSummary;
        bool isInitializing = true;

        void Awake()
        {
            EnsureUIToolkitInput();
        }

        void OnEnable()
        {
            EnsureUIToolkitInput();
            if (Application.isPlaying)
            {
                if (!app) app = FindAnyObjectByType<LunarApplication>();
                if (app != null && (root == null || root.childCount == 0))
                {
                    Initialize(app);
                }
            }
        }

        public void EnsureUIToolkitInput()
        {
            if (!Application.isPlaying) return;

            try
            {
                // Ensure InputSystem does not disable devices when window focus changes
                if (UnityEngine.InputSystem.InputSystem.settings != null &&
                    UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior != UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus)
                {
                    UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
                }

                var asm = typeof(UIDocument).Assembly;
                var uieType = asm.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility");
                if (uieType != null)
                {
                    // Ensure Play Mode flag is restored in UIElementsRuntimeUtility after any domain reload
                    var fPlayMode = uieType.GetField("s_IsPlayMode", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (fPlayMode != null && !(bool)fPlayMode.GetValue(null))
                    {
                        fPlayMode.SetValue(null, true);
                    }

                    var propOverride = uieType.GetProperty("overrideUseDefaultEventSystem", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    propOverride?.SetValue(null, true);

                    var propMode = uieType.GetProperty("eventSystemUpdateMode", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    var defEsType = asm.GetType("UnityEngine.UIElements.DefaultEventSystem");
                    var updateModeType = defEsType?.GetNestedType("UpdateMode", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (updateModeType != null) propMode?.SetValue(null, System.Enum.Parse(updateModeType, "Always"));

                    var defEs = uieType.GetProperty("defaultEventSystem", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.GetValue(null);
                    if (defEs != null && defEsType != null)
                    {
                        var propReady = defEsType.GetProperty("isInputReady", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (propReady != null && !(bool)propReady.GetValue(defEs))
                        {
                            var mInit = defEsType.GetMethod("InitInputProcessor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                            mInit?.Invoke(defEs, null);
                            propReady.SetValue(defEs, true);
                        }
                    }

                    var mCallbacks = uieType.GetMethod("EnableRenderingAndInputCallbacks", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    mCallbacks?.Invoke(null, null);
                }
            }
            catch { }
        }

        public void Initialize(LunarApplication value)
        {
            EnsureUIToolkitInput();
            isInitializing = true;
            assessedData = null;
            assessmentState = "Awaiting analysis";
            assessmentMessage = "Select a location and analyze your mission window.";
            app = value;

            var document=GetComponent<UIDocument>();
            if(Application.isPlaying && !runtimePanel)
            {
                originalPanel=document.panelSettings;runtimePanel=Instantiate(originalPanel);
                runtimePanel.scaleMode=PanelScaleMode.ConstantPixelSize;runtimePanel.scale=1;
                document.panelSettings=runtimePanel;
            }
            root = document.rootVisualElement;
            root.Clear();
            // UIDocument recreates its tree after a Play Mode script reload or re-enable.
            // Discard controls belonging to the previous tree before rebuilding.
            markers.Clear(); markerDots.Clear(); markerLines.Clear(); navButtons.Clear();
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
            WrapCard(floatingInspector,"inspector-scroll", 5);WrapCard(floatingMetrics,"results-panel-scroll", 1);
            missionConfigView.BtnAnalyze.RemoveFromHierarchy();floatingInspector.Add(missionConfigView.BtnAnalyze);
            opportunityFinderView.BtnSearch.RemoveFromHierarchy();floatingFinder.Add(opportunityFinderView.BtnSearch);
            btnReopenInspector = new Button(() => SetInspectorVisible(true)) { text = "+ Site Inspector", name = "btn-reopen-inspector" };
            btnReopenInspector.AddToClassList("chip-btn");
            btnReopenInspector.AddToClassList("btn-reopen-card");
            btnReopenInspector.style.position = Position.Absolute;
            btnReopenInspector.style.display = DisplayStyle.None;
            overlay.Add(btnReopenInspector);

            btnReopenMetrics = new Button(() => SetMetricsVisible(true)) { text = "+ Mission Assessment", name = "btn-reopen-metrics" };
            btnReopenMetrics.AddToClassList("chip-btn");
            btnReopenMetrics.AddToClassList("btn-reopen-card");
            btnReopenMetrics.style.position = Position.Absolute;
            btnReopenMetrics.style.display = DisplayStyle.None;
            overlay.Add(btnReopenMetrics);
            guidanceLabel = new Label("Click the Moon to choose a location, or select a NASA reference site.");
            guidanceLabel.name="navigation-guidance"; guidanceLabel.pickingMode=PickingMode.Ignore;
            guidanceLabel.style.position=Position.Absolute; guidanceLabel.style.top=80;
            guidanceLabel.style.left=Length.Percent(22); guidanceLabel.style.right=Length.Percent(22);
            guidanceLabel.style.whiteSpace=WhiteSpace.Normal;
            guidanceLabel.style.unityTextAlign=TextAnchor.MiddleCenter;
            guidanceLabel.style.fontSize=11; guidanceLabel.style.color=new Color(.75f,.85f,.9f);
            overlay.Add(guidanceLabel);
            root.RegisterCallback<GeometryChangedEvent>(_=>ApplyResponsiveLayout());ApplyResponsiveLayout();

            // Set initial state from analysis repository
            if (AnalysisRepository.Instance.HasData)
            {
                selectedLandingUtc = AnalysisRepository.Instance.T0;
                if (missionConfigView != null) missionConfigView.SetLandingUtc(selectedLandingUtc);
            }

            isInitializing = false;
            SetMode(AppMode.Inspect);
            UpdateStatusBadge();
            OnSiteChanged(app.SelectedSite);

            root.RegisterCallback<PointerDownEvent>(e =>
            {
                if (CurrentPopup != "" && e.target == root) Close();
            });
        }

        static void ConfigureScroll(ScrollView scroll)
        {
            scroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;scroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
            scroll.contentContainer.style.width=Length.Percent(100);scroll.contentContainer.style.minWidth=0;
            scroll.contentContainer.style.flexShrink=0;scroll.contentViewport.style.minWidth=0;
            var vs = scroll.verticalScroller;
            if (vs != null)
            {
                vs.style.width = 8; vs.style.minWidth = 8; vs.style.backgroundColor = Color.clear;
                vs.style.marginRight = 2; vs.style.marginTop = 2; vs.style.marginBottom = 2;
                if (vs.slider != null)
                {
                    vs.slider.style.width = 8; vs.slider.style.minWidth = 8;
                    vs.slider.style.marginLeft = vs.slider.style.marginRight = 0;
                    vs.slider.style.paddingLeft = vs.slider.style.paddingRight = 0;
                    var tracker = vs.slider.Q(className: "unity-base-slider__tracker");
                    if (tracker != null)
                    {
                        tracker.style.width = 4; tracker.style.minWidth = 4;
                        tracker.style.backgroundColor = new Color(1f, 1f, 1f, 0.12f);
                        tracker.style.borderTopWidth = tracker.style.borderBottomWidth = tracker.style.borderLeftWidth = tracker.style.borderRightWidth = 0;
                        tracker.style.borderTopLeftRadius = tracker.style.borderTopRightRadius = tracker.style.borderBottomLeftRadius = tracker.style.borderBottomRightRadius = 2;
                    }
                    var dragger = vs.slider.Q(className: "unity-base-slider__dragger");
                    if (dragger != null)
                    {
                        dragger.style.width = 8; dragger.style.minWidth = 8;
                        dragger.style.left = dragger.style.right = 0;
                        dragger.style.backgroundColor = new Color(0.35f, 0.48f, 0.62f, 0.95f);
                        dragger.style.borderTopWidth = dragger.style.borderBottomWidth = dragger.style.borderLeftWidth = dragger.style.borderRightWidth = 0;
                        dragger.style.borderTopLeftRadius = dragger.style.borderTopRightRadius = dragger.style.borderBottomLeftRadius = dragger.style.borderBottomRightRadius = 4;
                    }
                    var draggerBorder = vs.slider.Q(className: "unity-base-slider__dragger-border");
                    if (draggerBorder != null) draggerBorder.style.display = DisplayStyle.None;
                }
                var low = vs.Q(className: "unity-scroller__low-button"); if (low != null) low.style.display = DisplayStyle.None;
                var high = vs.Q(className: "unity-scroller__high-button"); if (high != null) high.style.display = DisplayStyle.None;
            }
        }
        void WrapCard(VisualElement card,string name, int pinCount = 1)
        {
            var children=new List<VisualElement>(card.Children());
            var scroll=new ScrollView(ScrollViewMode.Vertical){name=name};ConfigureScroll(scroll);
            scroll.style.flexGrow=1;scroll.style.minHeight=0;
            for(int i=pinCount;i<children.Count;i++){children[i].RemoveFromHierarchy();scroll.Add(children[i]);}
            card.Add(scroll);
        }
        void ApplyResponsiveLayout()
        {
            if(root==null)return;
            float w=root.layout.width>1?root.layout.width:Screen.width,h=root.layout.height>1?root.layout.height:Screen.height;
            float side=w<1250?290:w<1500?310:340;
            float cardTop = h <= 720 ? 76 : 96;
            bool compactTimeline = timelineBody != null && timelineBody.style.display == DisplayStyle.None;
            float cardBottom = compactTimeline ? 78 : h <= 720 ? 200 : 210;
            foreach(var card in new[]{floatingInspector,floatingMetrics,floatingFinder})
            {if(card==null)continue;card.style.width=side;card.style.top=cardTop;card.style.bottom=cardBottom;card.style.maxHeight=h>900?new StyleLength(680):new StyleLength(StyleKeyword.None);}
            floatingInspector.style.left=16;floatingFinder.style.left=16;floatingMetrics.style.right=16;
            if(btnReopenInspector!=null){btnReopenInspector.style.top=cardTop;btnReopenInspector.style.left=16;}
            if(btnReopenMetrics!=null){btnReopenMetrics.style.top=cardTop;btnReopenMetrics.style.right=16;}
            timelineCard.style.width=Mathf.Min(1000,w-32);timelineCard.style.bottom=12;
            if (surfaceOverlay != null)
            {
                // Keep the surface readouts in the free space between side cards,
                // above the timeline at every supported Game View size.
                surfaceOverlay.style.left = surfaceOverlay.style.right = side + 32;
                surfaceOverlay.style.bottom = cardBottom;
                surfaceOverlay.style.flexDirection = FlexDirection.Column;
                surfaceOverlay.style.alignItems = Align.Center;
                foreach (var readout in surfaceOverlay.Children())
                {
                    readout.style.width = Length.Percent(100);
                    readout.style.maxWidth = 480;
                    readout.style.marginTop = 6;
                }
            }
            if(guidanceLabel!=null)
            {
                guidanceLabel.style.top=80;
                guidanceLabel.style.left=Length.Percent(22);
                guidanceLabel.style.right=Length.Percent(22);
            }
            if(popupModal!=null)
            {
                float modalWidth=Mathf.Min(440,w-32);popupModal.style.width=modalWidth;popupModal.style.left=(w-modalWidth)/2;
                // Reserve the 174 px timeline, its 12 px bottom margin, the
                // modal's 74 px top offset and a 12 px separation.
                float modalHeight = Mathf.Max(180,h-cardBottom-86);
                popupModal.style.right=StyleKeyword.Auto;popupModal.style.top=74;popupModal.style.maxHeight=modalHeight;
                if(popupBody!=null)popupBody.style.maxHeight=Mathf.Max(100,modalHeight-65);
            }
        }
        void OnDestroy(){if(runtimePanel){GetComponent<UIDocument>().panelSettings=originalPanel;Destroy(runtimePanel);}}

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

            brandRow.Add(CreateBrandImage(30, "header-app-icon"));

            var title = new Label("LUNAR NAVIGATOR");
            title.AddToClassList("brand-title");
            brandRow.Add(title);

            // Provenance belongs to the current assessment, not the application brand.
            statusBadge = new VisualElement { name = "status-badge", pickingMode = PickingMode.Position };
            statusBadge.AddToClassList("status-badge");
            statusBadgeText = new Label("●  ILLUSTRATIVE DEMO");
            statusBadgeText.AddToClassList("status-badge-text");
            statusBadge.Add(statusBadgeText);
            // Attached to the assessment card when that card is built.

            var sub = new Label("SOUTH-POLE LANDING SITE EVALUATION");
            sub.AddToClassList("eyebrow");
            brandBlock.Add(sub);

            // Center: Mode Switch
            modeSwitchContainer = new VisualElement { name = "mode-switch", pickingMode = PickingMode.Ignore };
            modeSwitchContainer.AddToClassList("mode-switch-container");
            headerBar.Add(modeSwitchContainer);

            btnModeInspect = new Button(() => SetMode(AppMode.Inspect)) { text = "ANALYZE THIS MISSION" };
            btnModeInspect.AddToClassList("mode-pill");
            modeSwitchContainer.Add(btnModeInspect);

            btnModeFind = new Button(() => SetMode(AppMode.Find)) { text = "FIND BEST LANDING WINDOWS" };
            btnModeFind.AddToClassList("mode-pill");
            modeSwitchContainer.Add(btnModeFind);

            // Right: Nav pills
            navPills = new VisualElement { name = "nav-pills", pickingMode = PickingMode.Ignore };
            navPills.AddToClassList("nav-pills");
            headerBar.Add(navPills);

            foreach (var label in new[] { "Choose site", "View options", "Guide" })
            {
                var pill = new Button(() => Open(label)) { text = label, name = "nav-" + label.Replace(" ", "-").ToLowerInvariant() };
                pill.tooltip = label == "Choose site" ? "Select and focus a NASA reference location" :
                    label == "View options" ? "Camera, markers, lighting and graphics" : "Getting started, controls and result meanings";
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

            var closeBtn = new Button(() => SetInspectorVisible(false)) { text = "×" };
            closeBtn.AddToClassList("icon-btn");
            head.Add(closeBtn);
            floatingInspector.Add(head);

            // Site selector
            var siteChoices = new List<string>();
            foreach (var s in app.sites) siteChoices.Add(s.displayName.Split((char)40)[0].Trim());
            siteChoices.Add("Custom location");
            siteDropdown = new DropdownField("", siteChoices, 0);
            siteDropdown.RegisterValueChangedCallback(e =>
            {
                if (e.newValue == "Custom location")
                {
                    if (app.CustomSite)
                    {
                        app.SelectSite(app.CustomSite);
                    }
                    else
                    {
                        double lat = app.SelectedSite != null ? app.SelectedSite.latitude : -89.5;
                        double lon = app.SelectedSite != null ? app.SelectedSite.longitude : 0.0;
                        app.SelectCustom(lat, lon);
                    }
                    return;
                }
                int idx = siteChoices.IndexOf(e.newValue);
                if(idx<0)idx=Array.FindIndex(app.sites,s=>s.displayName==e.newValue);
                if (idx >= 0 && idx < app.sites.Length)
                {
                    app.SelectSite(app.sites[idx]);
                    RunAnalysis();
                }
            });
            floatingInspector.Add(new Label("1  Location"){name="location-field-label"});
            floatingInspector.Add(siteDropdown);
            selectionLabel = new Label(); selectionLabel.style.whiteSpace = WhiteSpace.Normal;
            floatingInspector.Add(selectionLabel);
            analysisFeedback = new Label("Select a location and mission window.");
            analysisFeedback.style.whiteSpace = WhiteSpace.Normal;
            analysisFeedback.style.color = new Color(.35f,.9f,.75f);
            floatingInspector.Add(analysisFeedback);
            RefreshSelectionLabel();

            // Mission Configuration (Editable UTC landing time, duration in hours, online API client)
            missionConfigView = new MissionConfigView();
            missionConfigView.SetLandingUtc(selectedLandingUtc);
            missionConfigView.SetDuration(selectedDurationHours);
            missionConfigView.OnConfigChanged += (utc, durationH) =>
            {
                selectedLandingUtc = utc;
                selectedDurationHours = durationH;
                if (!isInitializing) InvalidateAssessment(true);
                RunAnalysis();
            };
            missionConfigView.OnAnalyzeRequested += (utc, durationH) =>
            {
                selectedLandingUtc = utc;
                selectedDurationHours = durationH;
                RequestOnlineAnalysis(utc, durationH);
            };
            missionConfigView.OnCancelRequested += () =>
            {
                LunarApiClient.Instance.CancelCurrentRequest();
                InvalidateAssessment(true);
            };
            missionConfigView.TxtLandingUtc.RegisterValueChangedCallback(_ => CheckAssessmentInput());
            missionConfigView.TxtDurationHours.RegisterValueChangedCallback(_ => CheckAssessmentInput());
            floatingInspector.Add(missionConfigView);
            var scientificInfo=new Foldout{text="Scientific information",value=false};
            scientificInfo.Add(new Label("GEOMETRIC_ONLY: NASA/JPL Horizons, airless target-center angles and a spherical lunar horizon. The observer is 2 m above the nominal sphere. No terrain obstruction, power, RF link or touchdown certification. Visual terrain is illustrative."));
            scientificInfo.Add(new Label(){name="selected-provenance"});
            floatingInspector.Add(scientificInfo);

            // Mission Requirements Foldout
            var reqFoldout = new Foldout { text = "Mission Requirements", value = false };
            reqFoldout.style.marginTop = 6;
            reqFoldout.Add(new Label("Set minimum sunlight and communication availability for your mission. These targets determine the PASS/FAIL labels in Mission Results."));
            sliderMinSun = new Slider("Min Sunlit %", 0, 100) { value = 30 }; reqFoldout.Add(sliderMinSun);
            sliderMinEarth = new Slider("Min Earth %", 0, 100) { value = 40 }; reqFoldout.Add(sliderMinEarth);
            sliderMaxDark = new Slider("Max Dark (h)", 0, 72) { value = 36 }; reqFoldout.Add(sliderMaxDark);
            sliderMaxComm = new Slider("Max Comm Gap (h)", 0, 72) { value = 48 }; reqFoldout.Add(sliderMaxComm);
            foreach(var slider in new[]{sliderMinSun,sliderMinEarth,sliderMaxDark,sliderMaxComm})
            {slider.showInputField=true;slider.RegisterValueChangedCallback(_=>RunAnalysis());}
            var note = new Label("User-selected requirements, not NASA mission standards.");
            note.AddToClassList("subtle");
            reqFoldout.Add(note);
            floatingInspector.Add(reqFoldout);

            btnAddCompare = new Button(ToggleComparison) { text = hasComparison ? "Remove Comparison" : "+ Compare +2 days",tooltip="Requires loaded coverage for this window and a landing two days later" };
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
            var head = new VisualElement(); head.AddToClassList("card-header");
            var title = new Label("MISSION ASSESSMENT"); title.AddToClassList("card-title");head.Add(title);
            var closeBtn = new Button(() => SetMetricsVisible(false)) { text = "×" };
            closeBtn.AddToClassList("icon-btn");head.Add(closeBtn);floatingMetrics.Add(head);
            floatingMetrics.Add(statusBadge);
            skylineChartElement = new SkylineChartElement();
            assessmentView = new MissionAssessmentView(skylineChartElement);
            floatingMetrics.Add(assessmentView);
            lblMetricSun=assessmentView.SunValue;lblMetricEarth=assessmentView.EarthValue;
            lblMetricDark=assessmentView.DarkValue;lblMetricComm=assessmentView.GapValue;
            lblStatusSun=assessmentView.SunStatus;lblStatusEarth=assessmentView.EarthStatus;
            lblStatusDark=assessmentView.DarkLimit;lblStatusComm=assessmentView.GapLimit;
            lblMetricBoth=assessmentView.JointLabel;lblMetricsCaption=assessmentView.Limitation;
            resultsInterpretation=assessmentView.Conclusion;
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
            floatingFinder.style.bottom=210;
            var finderScroll = new ScrollView(ScrollViewMode.Vertical);
            finderScroll.name="finder-panel-scroll";
            finderScroll.style.flexGrow=1;
            finderScroll.style.minHeight=0;ConfigureScroll(finderScroll);
            opportunityFinderView.style.flexShrink=0;
            finderScroll.Add(opportunityFinderView);
            floatingFinder.Add(finderScroll);

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
            btnTimelineToggle = new Button(ToggleTimelineCollapse) { text = "Mission timeline" };
            btnTimelineToggle.AddToClassList("chip-btn");
            leftControls.Add(btnTimelineToggle);

            btnPlayPause = new Button(() => app.Clock.Toggle()) { text = "▶" };
            btnPlayPause.AddToClassList("chip-btn");btnPlayPause.name="timeline-play";btnPlayPause.tooltip="Play mission timeline";
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
            timelineBody.style.display = DisplayStyle.None;
            btnTimelineToggle.text = "Show timeline";
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
            var note = new Label("Foreground terrain is illustrative. Visual camera height does not change the 2 m scientific observer.");
            note.style.whiteSpace=WhiteSpace.Normal;
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
            popupScrim=new VisualElement{name="popup-scrim"};
            popupScrim.style.position=Position.Absolute;popupScrim.style.left=0;popupScrim.style.right=0;popupScrim.style.top=0;popupScrim.style.bottom=0;
            popupScrim.style.backgroundColor=new Color(0,0,0,.38f);popupScrim.style.display=DisplayStyle.None;
            popupScrim.RegisterCallback<PointerDownEvent>(e=>{if(e.target==popupScrim)Close();});
            overlay.Add(popupScrim);popupScrim.Add(popupModal);
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
                marker.text=site.displayName.Split((char)40)[0].Trim();marker.tooltip=site.displayName;
                marker.AddToClassList("marker-pin");
                marker.RegisterCallback<ClickEvent>(e => { if (e.clickCount == 2) app.Director.Focus(); });
                markerLayer.Add(marker); markers.Add(marker);
            }

            earthMarkerDot = new VisualElement { name = "earth-marker-dot" };
            earthMarkerDot.AddToClassList("marker-point");
            earthMarkerDot.pickingMode = PickingMode.Ignore;
            earthMarkerDot.style.backgroundColor = new Color(0.35f, 0.7f, 1f, 1f);
            markerLayer.Add(earthMarkerDot);

            earthMarker = new Button(() => { app.Director.LookAtEarth(); }) { text = "Earth ♁" };
            earthMarker.tooltip = "Earth · Press E or click to look directly at Earth";
            earthMarker.AddToClassList("marker-pin");
            earthMarker.style.borderTopColor = earthMarker.style.borderBottomColor = earthMarker.style.borderLeftColor = earthMarker.style.borderRightColor = new Color(0.35f, 0.7f, 1f, 0.8f);
            earthMarker.style.color = new Color(0.7f, 0.9f, 1f, 1f);
            markerLayer.Add(earthMarker);

            sunMarkerDot = new VisualElement { name = "sun-marker-dot" };
            sunMarkerDot.AddToClassList("marker-point");
            sunMarkerDot.pickingMode = PickingMode.Ignore;
            sunMarkerDot.style.backgroundColor = new Color(1f, 0.82f, 0.28f, 1f);
            markerLayer.Add(sunMarkerDot);

            sunMarker = new Button(() => { app.Director.LookAtSun(); }) { text = "Sun ☉" };
            sunMarker.tooltip = "Sun · Click (or Shift+S) to look directly at Sun";
            sunMarker.AddToClassList("marker-pin");
            sunMarker.style.borderTopColor = sunMarker.style.borderBottomColor = sunMarker.style.borderLeftColor = sunMarker.style.borderRightColor = new Color(1f, 0.82f, 0.28f, 0.8f);
            sunMarker.style.color = new Color(1f, 0.92f, 0.65f, 1f);
            markerLayer.Add(sunMarker);
        }

        public void SetInspectorVisible(bool visible)
        {
            inspectorVisible = visible;
            if (floatingInspector != null)
                floatingInspector.style.display = (visible && InterfaceVisible && Mode == AppMode.Inspect) ? DisplayStyle.Flex : DisplayStyle.None;
            if (btnReopenInspector != null)
                btnReopenInspector.style.display = (!visible && InterfaceVisible && Mode == AppMode.Inspect) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetMetricsVisible(bool visible)
        {
            metricsVisible = visible;
            if (floatingMetrics != null)
            {
                floatingMetrics.RemoveFromClassList("hidden");
                floatingMetrics.style.display = (visible && InterfaceVisible) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (btnReopenMetrics != null)
                btnReopenMetrics.style.display = (!visible && InterfaceVisible) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetMode(AppMode newMode)
        {
            Mode = newMode;
            UpdateStatusBadge();
            if (btnModeInspect != null) btnModeInspect.EnableInClassList("mode-pill-active", Mode == AppMode.Inspect);
            if (btnModeFind != null) btnModeFind.EnableInClassList("mode-pill-active", Mode == AppMode.Find);

            if (Mode == AppMode.Inspect)
            {
                SetInspectorVisible(true);
                SetMetricsVisible(true);
                if (guidanceLabel != null) guidanceLabel.text = "Click the Moon to choose a location, or select a NASA reference site.";
            }
            else if (Mode == AppMode.Find)
            {
                SetInspectorVisible(false);
                if (btnReopenInspector != null) btnReopenInspector.style.display = DisplayStyle.None;
                if (guidanceLabel != null) guidanceLabel.text = "Compare landing windows across NASA reference sites.";
            }

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
            if (InterfaceVisible)
            {
                SetInspectorVisible(inspectorVisible);
                SetMetricsVisible(metricsVisible);
            }
            else
            {
                if (floatingInspector != null) floatingInspector.style.display = DisplayStyle.None;
                if (floatingMetrics != null) floatingMetrics.style.display = DisplayStyle.None;
                if (btnReopenInspector != null) btnReopenInspector.style.display = DisplayStyle.None;
                if (btnReopenMetrics != null) btnReopenMetrics.style.display = DisplayStyle.None;
            }
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
            if (guidanceLabel != null)
                guidanceLabel.style.display = InterfaceVisible && CurrentPopup == "" ? DisplayStyle.Flex : DisplayStyle.None;
            Close();
            if(popupScrim!=null)popupScrim.style.display=DisplayStyle.None;
        }

        public bool Close()
        {
            if (CurrentPopup != "")
            {
                CurrentPopup = "";ignoreWorldPickUntil=Time.unscaledTime+.15f;
                foreach (var button in navButtons.Values) button.RemoveFromClassList("nav-pill-active");
                popupModal.style.display = DisplayStyle.None;popupScrim.style.display=DisplayStyle.None;
                if (guidanceLabel != null) guidanceLabel.style.display = InterfaceVisible ? DisplayStyle.Flex : DisplayStyle.None;
                return true;
            }
            return false;
        }

        public void Open(string title)
        {
            if (title == "Sites") title = "Choose site";
            if (title == "Layers" || title == "Settings") title = "View options";
            if (title == "Help") title = "Guide";
            if (CurrentPopup == title) { Close(); return; }
            CurrentPopup = title;popupScrim.style.display=DisplayStyle.Flex;popupScrim.BringToFront();
            headerBar.BringToFront(); // Keep menu switching and the active menu accessible.
            foreach (var item in navButtons) item.Value.EnableInClassList("nav-pill-active", item.Key == title);
            if (guidanceLabel != null) guidanceLabel.style.display = DisplayStyle.None;
            popupModal.Clear();
            popupModal.style.display = DisplayStyle.Flex;

            var head = new VisualElement(); head.AddToClassList("card-header");
            head.Add(new Label(title.ToUpperInvariant()) { name = "modal-title" });
            head.Add(new Button(() => Close()) { text = "×" });
            head.Q<Button>().AddToClassList("icon-btn");
            popupModal.Add(head);popupBody=new ScrollView(ScrollViewMode.Vertical){name="popup-body"};ConfigureScroll(popupBody);
            popupModal.Add(popupBody);ApplyResponsiveLayout();

            if (title == "Choose site")
            {
                AddPopupIntro("Choose a reference location to inspect. Set your mission window, then analyze when ready.");
                foreach (var site in app.sites)
                {
                    var target = site;
                    var button = new Button(() => {
                        app.SelectSite(target);
                        SetMode(AppMode.Inspect);
                        app.Director.ChangeMode(CameraMode.Approach);
                        Close();
                    }) {
                        text = $"{target.displayName.Split('(')[0].Trim()}\n{target.latitude:F2}°, {target.longitude:F2}° E",
                        tooltip = target.description
                    };
                    button.AddToClassList("btn-secondary");
                    button.AddToClassList("popup-action");
                    popupBody.Add(button);
                }
                AddPopupIntro("For a custom location, close this panel and click the Moon. The cyan beacon marks your selection.");
            }
            else if (title == "View options") BuildViewOptions();
            else if (title == "Guide") BuildGuide();
            else if (title == "About") BuildAbout();
            else if (title == "Simulation")
            {
                var speedLabel = new Label("SIMULATION CONTROLS"); speedLabel.AddToClassList("eyebrow"); popupBody.Add(speedLabel);
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
                popupBody.Add(row);
                var info = new Label($"CURRENT UTC: {app.Clock.Utc:yyyy-MM-dd HH:mm:ss} UTC");
                info.AddToClassList("subtle");
                info.style.marginTop = 8;
                popupBody.Add(info);
            }
            else if (title == "Site information")
            {
                var s = app.SelectedSite;
                if (s != null)
                {
                    var nameLbl = new Label(s.displayName); nameLbl.AddToClassList("metric-value"); popupBody.Add(nameLbl);
                    var coords = new Label($"Coordinates: {s.latitude:F2}°, {s.longitude:F2}° (Elev: {s.elevation:F1}m)"); coords.AddToClassList("subtle"); popupBody.Add(coords);
                    var desc = new Label(s.description); desc.AddToClassList("subtle"); desc.style.marginTop = 6; popupBody.Add(desc);
                    var prov = new Label($"Provenance: {s.provenance}"); prov.AddToClassList("subtle"); prov.style.marginTop = 6; popupBody.Add(prov);
                }
            }
        }

        void AddPopupIntro(string text)
        {
            var label = new Label(text);
            label.AddToClassList("popup-intro");
            popupBody.Add(label);
        }

        static Image CreateBrandImage(int size, string elementName)
        {
            var image = new Image {
                name = elementName,
                image = Resources.Load<Texture2D>("Branding/LunarNavigator"),
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };
            image.style.width = size; image.style.height = size;
            image.style.flexShrink = 0; image.style.marginRight = 10;
            return image;
        }

        void BuildAbout()
        {
            var image = CreateBrandImage(112, "about-app-icon");
            image.style.alignSelf = Align.Center;
            image.style.marginBottom = 12;
            popupBody.Add(image);
            AddPopupSection("LUNAR NAVIGATOR");
            AddPopupIntro("Version " + Application.version + "\nLunar south-pole mission planning and visualization.");
            AddPopupIntro("Scientific ephemerides: NASA/JPL Horizons. Geometric results use a spherical lunar horizon. Visible terrain is illustrative and is not a terrain-validated landing safety assessment.");
            AddPopupIntro("LunarNavigator Team\nBuilt with Unity 6 HDRP and a Python scientific backend.");
        }

        void AddPopupSection(string text)
        {
            var label = new Label(text);
            label.AddToClassList("popup-section");
            popupBody.Add(label);
        }

        void BuildViewOptions()
        {
            AddPopupIntro("Adjust your view of the Moon. Display options do not change the scientific analysis.");
            AddPopupSection("CAMERA");
            var cameraRow = new VisualElement(); cameraRow.AddToClassList("popup-choice-row");
            foreach (var mode in new[] { CameraMode.Global, CameraMode.Approach, CameraMode.LocalOrbit, CameraMode.Surface })
            {
                var target = mode;
                string label = mode == CameraMode.Global ? "Overview" : mode == CameraMode.LocalOrbit ? "Local orbit" : mode.ToString();
                var button = new Button(() => { app.Director.ChangeMode(target); Close(); }) { text = label };
                button.AddToClassList("chip-btn");
                button.EnableInClassList("chip-btn-active", app.Director.Mode == mode);
                cameraRow.Add(button);
            }
            popupBody.Add(cameraRow);
            var focus = new Button(() => { app.Director.Focus(); Close(); }) { text = "Focus selected location  (F)" };
            focus.AddToClassList("btn-secondary"); focus.AddToClassList("popup-action"); popupBody.Add(focus);
            var reset = new Button(() => { app.Director.ResetView(); Close(); }) { text = "Reset south-pole view  (R)" };
            reset.AddToClassList("btn-secondary"); reset.AddToClassList("popup-action"); popupBody.Add(reset);
            AddPopupSection("MAP & LIGHTING");
            AddToggle("Reference site labels", app.World.markersVisible, v => app.World.markersVisible = v);
            AddToggle("Sun and Earth direction guides", app.World.aidsVisible, v => app.World.aidsVisible = v);
            AddToggle("Star background", app.World.stars.activeSelf, v => app.World.stars.SetActive(v));
            AddToggle("Brighten shadowed terrain for exploration", app.World.terrainVisibilityAid, v => app.World.terrainVisibilityAid = v);
            var advanced = new Foldout { text = "Graphics & playback", value = false };
            popupBody.Add(advanced);
            var graphics = new VisualElement(); graphics.AddToClassList("popup-choice-row");
            var graphicsButtons = new List<Button>();
            foreach (var preset in new[] { "Performance", "Balanced", "Cinematic" })
            {
                var target = preset;
                var button = new Button(() => {
                    app.World.ApplyQuality(target);
                    foreach (var item in graphicsButtons) item.EnableInClassList("chip-btn-active", item.text == app.World.Preset);
                }) { text = preset };
                button.AddToClassList("chip-btn"); button.EnableInClassList("chip-btn-active", app.World.Preset == preset);
                graphicsButtons.Add(button); graphics.Add(button);
            }
            advanced.Add(new Label("Graphics quality")); advanced.Add(graphics);
            advanced.Add(new Label("Timeline playback speed"));
            var speeds = new VisualElement(); speeds.AddToClassList("popup-choice-row");
            var speedButtons = new List<Button>();
            foreach (double speed in new double[] { 1, 60, 3600, 86400 })
            {
                var target = speed;
                var button = new Button(() => {
                    app.Clock.speed = target;
                    foreach (var item in speedButtons) item.EnableInClassList("chip-btn-active", item.userData is double value && value == target);
                }) { text = speed == 1 ? "Real time" : speed == 60 ? "1 min / sec" : speed == 3600 ? "1 h / sec" : "1 day / sec", userData = speed };
                button.AddToClassList("chip-btn"); button.EnableInClassList("chip-btn-active", app.Clock.speed == speed);
                speedButtons.Add(button); speeds.Add(button);
            }
            advanced.Add(speeds);
        }

        void BuildGuide()
        {
            var about = new Button(() => Open("About")) { text = "About Lunar Navigator", name = "btn-about" };
            about.AddToClassList("btn-secondary"); about.AddToClassList("popup-action");
            popupBody.Add(about);
            AddPopupSection("START HERE");
            foreach (var text in new[] {
                "1. Choose a reference site, or click the Moon for a custom location.",
                "2. Set the landing date (UTC) and surface stay, then select Analyze mission window.",
                "3. Read sunlight, Earth visibility and mission requirements in Mission Assessment.",
                "To compare landing dates, switch to Find best landing windows. This searches reference sites only."
            }) { var label = new Label(text); label.AddToClassList("guide-copy"); popupBody.Add(label); }
            var controls = new Foldout { text = "Mouse & keyboard controls", value = true };
            popupBody.Add(controls);
            foreach (var entry in new[] {
                new[] { "1 / 2 / 3 / 4", "Overview / Approach / Local orbit / Surface" },
                new[] { "W A S D", "Move in Local orbit and Surface; hold Shift to move faster" },
                new[] { "Left drag", "Rotate the camera or look around" },
                new[] { "Scroll", "Zoom over the Moon; scroll panels over the interface" },
                new[] { "Ctrl + scroll", "Optical zoom without changing the viewing angle" },
                new[] { "F / R / E", "Focus selection / reset view / look at Earth (Shift+S for Sun)" },
                new[] { "Space", "Play or pause the mission timeline" },
                new[] { "Left / Right", "Step timeline by one hour" },
                new[] { "Tab / H / Esc", "Toggle interface / open guide / go back" }
            }) {
                var row = new VisualElement(); row.AddToClassList("shortcut-row");
                var key = new Label(entry[0]); key.AddToClassList("shortcut-key"); row.Add(key);
                var description = new Label(entry[1]); description.AddToClassList("shortcut-description"); row.Add(description);
                controls.Add(row);
            }
            var meanings = new Foldout { text = "Understanding results", value = false };
            var meaning = new Label("Awaiting analysis means this location and mission window have no current results. Changing either requires a new analysis. Geometric results use NASA Sun and Earth positions with a spherical lunar horizon. They exclude terrain obstruction, power and radio link budgets, and landing-safety certification. Local terrain is illustrative.");
            meaning.AddToClassList("guide-copy"); meanings.Add(meaning); popupBody.Add(meanings);
        }

        private void AddToggle(string label, bool val, Action<bool> onChange)
        {
            var t = new Toggle { text = label, value = val };
            t.RegisterValueChangedCallback(e => onChange(e.newValue));
            t.AddToClassList("popup-setting");
            popupBody.Add(t);
        }

        void RefreshSelectionLabel()
        {
            var site = app?.SelectedSite;
            var source=root?.Q<Label>("selected-provenance");if(source!=null && site!=null)source.text=site.provenance;
            if (selectionLabel != null && site != null)
                selectionLabel.text = $"Lat {site.AnalysisLatitude:F4}°  /  Lon {site.AnalysisLongitude:F4}° E";
        }
        public bool IsEditingText
        {
            get
            {
                var focus = root?.panel?.focusController?.focusedElement as VisualElement;
                return focus is TextField || focus?.GetFirstAncestorOfType<TextField>() != null;
            }
        }
        public void OnSiteChanged(LunarSite site)
        {
            if (missionConfigView?.IsLoading == true) LunarApiClient.Instance.CancelCurrentRequest();
            missionConfigView?.SetLoading(false);
            missionConfigView?.ClearError();
            InvalidateAssessment(false);
            RefreshSelectionLabel();
            if (analysisFeedback != null) analysisFeedback.text = "Ready to analyze this location.";
            UpdateStatusBadge();
            if (siteDropdown != null && site != null)
            {
                siteDropdown.SetValueWithoutNotify(site.IsCustom?"Custom location":site.displayName.Split((char)40)[0].Trim());
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

        void CheckAssessmentInput()
        {
            if (isInitializing || missionConfigView == null) return;
            if (!DateTime.TryParse(missionConfigView.TxtLandingUtc.value,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal|DateTimeStyles.AdjustToUniversal,out _) ||
                !double.TryParse(missionConfigView.TxtDurationHours.value,NumberStyles.Float,CultureInfo.InvariantCulture,out var hours) || double.IsNaN(hours) || hours<1 || hours>744)
            {
                InvalidateAssessment(true);
                assessmentMessage="Enter a valid UTC date and a mission duration of 1–744 hours, then analyze again.";
                ClearAnalysisDisplay();
            }
        }
        void InvalidateAssessment(bool outdated)
        {
            assessedData=null;
            if (missionConfigView?.IsLoading == true) { LunarApiClient.Instance.CancelCurrentRequest();missionConfigView.SetLoading(false); }
            assessmentState=outdated?"Results outdated / need reanalysis":"Awaiting analysis";
            assessmentMessage=outdated?"The mission has changed. Analyze this window to obtain current results.":"Analyze this location and mission window to assess your requirements.";
            if (assessmentView != null) ClearAnalysisDisplay();
        }
        void AssessmentFailed(string message)
        {
            assessedData=null;assessmentState="Analysis failed";
            assessmentMessage="No current assessment. Check the connection or mission inputs and retry.";
            if(assessmentView!=null)assessmentView.Outcome.tooltip=message;
            ClearAnalysisDisplay();
        }
        void ClearAnalysisDisplay()
        {
            if (assessmentView == null) return;
            lblMetricSun.text = lblMetricEarth.text = lblMetricDark.text = lblMetricComm.text = "--";
            lblMetricBoth.text = "No evaluated results for this location/window. Click Analyze.";
            lblMetricsCaption.text = "GEOMETRIC ONLY — terrain obstruction excluded.";
            if(resultsInterpretation!=null)resultsInterpretation.text="Analyze the selected mission to see results.";
            foreach(var label in new[]{lblStatusSun,lblStatusEarth,lblStatusDark,lblStatusComm})
            {
                label.text="Not evaluated";
                label.RemoveFromClassList("metric-cell-status-fail");
            }
            timelineElement?.SetData(null,0,0,selectedDurationHours);
            skylineChartElement?.SetData(null,0,0,selectedDurationHours);
            if (app?.SelectedSite != null)
                assessmentView.SetTarget(app.SelectedSite.displayName.Split('(')[0].Trim(), $"{app.SelectedSite.latitude:F2}°, {app.SelectedSite.longitude:F2}°");
            assessmentView.ShowUnavailable(assessmentState,assessmentMessage);
            UpdateStatusBadge();
            if (timelineBody != null) { timelineBody.style.display = DisplayStyle.None; btnTimelineToggle.text = "Show timeline"; ApplyResponsiveLayout(); }
            assessmentView.SetLimits(sliderMinSun?.value??30,sliderMinEarth?.value??40,sliderMaxDark?.value??36,sliderMaxComm?.value??48);
        }

        public void RunAnalysis()
        {
            if (isInitializing || lblMetricSun == null) return;
            var siteData = AnalysisRepository.Instance.GetSite(app?.SelectedSite?.id);
            if (siteData == null || !object.ReferenceEquals(siteData,assessedData) || assessedSiteId!=app.SelectedSite.id ||
                assessedLandingUtc!=selectedLandingUtc || Math.Abs(assessedDurationHours-selectedDurationHours)>1e-9)
            {
                ClearAnalysisDisplay();
                return;
            }

            DateTime t0 = AnalysisRepository.Instance.T0;
            double secFromT0 = (selectedLandingUtc.ToUniversalTime() - t0).TotalSeconds;
            double landingSec = Math.Max(0, secFromT0);

            try
            {
                Metrics m;
                if (siteData.metrics != null && Math.Abs((selectedLandingUtc.ToUniversalTime() - t0).TotalSeconds) < .001 && Math.Abs(siteData.metrics.duration_h - selectedDurationHours) < .000000001)
                {
                    m = new Metrics
                    {
                        start_utc = siteData.metrics.start_utc,
                        duration_h = siteData.metrics.duration_h,
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
                        lblMetricsCaption.text = "GEOMETRIC_ONLY - terrain obstruction excluded";
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

                assessmentView.Present(m,minSun,minEarth,maxDark,maxComm,siteData.data_status);
                if (app?.SelectedSite != null)
                    assessmentView.SetTarget(app.SelectedSite.displayName.Split('(')[0].Trim(), $"{app.SelectedSite.latitude:F2}°, {app.SelectedSite.longitude:F2}°");

                // Update Timeline and Skyline Chart
                timelineElement?.SetData(siteData, app.Clock.seconds, landingSec, selectedDurationHours);
                skylineChartElement?.SetData(siteData, app.Clock.seconds, landingSec, selectedDurationHours);

                if (hasComparison)
                {
                    try {
                    var comp = VisibilityMath.ComputeForUtc(siteData, compareLandingUtc, selectedDurationHours);
                    float dSun = (float)((m.f_sun - comp.f_sun) * 100.0);
                    float dDark = (float)(m.dark_max_h - comp.dark_max_h);
                    lblCompareSummary.text = $"Comparison: Sunlit {(dSun >= 0 ? "+" : "")}{dSun:F1} pts | Max Dark {(dDark >= 0 ? "+" : "")}{dDark:F1} h";
                    } catch(ArgumentException) {lblCompareSummary.text="Comparison needs loaded coverage for both this mission and a landing two days later. Current results are unchanged.";}
                }
            }
            catch (Exception ex)
            {
                assessmentState="Results outdated / need reanalysis";
                assessmentMessage="This window is outside the loaded coverage. Analyze it again.";
                ClearAnalysisDisplay();
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
                btnAddCompare.text = "+ Compare +2 days";
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
            oppResultsContainer.Add(new Label("No live search yet. Set your requirements and search for genuine NASA results.") { name="opp-empty" });
        }

        public void RequestOnlineOpportunities(LiveOpportunityRequest req)
        {
            if (opportunityFinderView == null) return;
            opportunityFinderView.ResultsScrollView.Clear();
            opportunityFinderView.ResultsScrollView.Add(new Label("Searching - no new recommendations yet."));
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
            selectedDurationHours = opp.mission_duration_h;
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
            if (selectedSite == null) { missionConfigView.ShowError("Select a location first."); return; }
            bool isCustomSite = selectedSite.IsCustom || (!string.IsNullOrEmpty(selectedSite.id) && selectedSite.id.StartsWith("custom"));
            if (isCustomSite)
            {
                missionConfigView.SetLoading(false);
                if (Math.Abs(selectedSite.AnalysisLatitude) >= 90)
                {
                    missionConfigView.ShowError("The API excludes exact poles. Pick a point away from +/-90 degrees.");
                    return;
                }
                missionConfigView.SetLoading(true, "Obtaining NASA data for this unverified custom coordinate...");
            }
            int requestedRevision = app.SelectionRevision;
            assessmentState="Analyzing...";assessmentMessage="Obtaining geometric Sun and Earth visibility from the scientific service.";
            ClearAnalysisDisplay();
            if (analysisFeedback != null) analysisFeedback.text = "LOADING: obtaining NASA data for " + selectedSite.displayName;
            UpdateStatusBadge();
            var payload = new AnalyzeRequestPayload
            {
                site_id = isCustomSite ? null : selectedSite.id,
                latitude_deg = selectedSite.AnalysisLatitude,
                longitude_deg = selectedSite.AnalysisLongitude,
                landing_utc = landingUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                mission_duration_h = durationH,
                observer_height_m = 2.0,
                sampling_step_minutes = 10
            };

            LunarApiClient.Instance.PostAnalyze(payload, (success, jsonResponse, errorMsg) =>
            {
                if (app.SelectionRevision != requestedRevision)
                {
                    missionConfigView?.SetLoading(false);
                    return; // Never apply another location's response.
                }
                if (selectedLandingUtc != landingUtc || selectedDurationHours != durationH)
                {
                    missionConfigView.SetLoading(false);
                    if (analysisFeedback != null) analysisFeedback.text = "Mission changed during request. Analyze the new window.";
                    InvalidateAssessment(true);
                    return;
                }
                if (success)
                {
                    missionConfigView.SetLoading(false);
                    missionConfigView.ClearError();

                    var adapted = ScienceApiAdapter.AdaptApiResponse(
                        jsonResponse,
                        selectedSite?.id,
                        selectedSite?.displayName,
                        out string adaptError);

                    if (adapted != null && (adapted.site.id != selectedSite.id ||
                        Math.Abs(adapted.site.lat_deg-selectedSite.AnalysisLatitude) > .0001 ||
                        Math.Abs(adapted.site.lon_east_deg-selectedSite.AnalysisLongitude) > .0001))
                    {
                        missionConfigView.ShowError("Response location does not match selected target. Data rejected.");
                        AssessmentFailed("Response location does not match selected target.");
                        return;
                    }
                    if (adapted != null)
                    {
                        AnalysisRepository.Instance.SetDynamicAnalysis(adapted);
                        assessedData=adapted;assessedSiteId=selectedSite.id;assessedLandingUtc=landingUtc;assessedDurationHours=durationH;
                        assessmentState="Analysis ready";
                        timelineBody.style.display=DisplayStyle.Flex;btnTimelineToggle.text="Hide tracks";ApplyResponsiveLayout();
                        app.Landmark?.Pulse();
                        if (analysisFeedback != null)
                        {
                            string sunMeaning = adapted.metrics.f_sun >= 1 ? "Sun above horizon throughout" : adapted.metrics.f_sun <= 0 ? "Sun hidden throughout" : $"Sun visible {adapted.metrics.f_sun:P0} of mission";
                            string earthMeaning = adapted.metrics.f_earth >= 1 ? "Earth visible throughout" : adapted.metrics.f_earth <= 0 ? "Earth hidden throughout" : $"Earth visible {adapted.metrics.f_earth:P0} of mission";
                            analysisFeedback.text = "Analysis ready";
                            analysisFeedback.tooltip=$"{selectedSite.displayName}: {sunMeaning}; {earthMeaning}. GEOMETRIC_ONLY.";
                        }
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
                        AssessmentFailed(adaptError);
                    }
                }
                else
                {
                    missionConfigView.SetLoading(false);
                    missionConfigView.ShowError(errorMsg);
                    if (analysisFeedback != null) analysisFeedback.text = "ANALYSIS FAILED - previous data is not a new result.";
                    AssessmentFailed(errorMsg);
                    UpdateStatusBadge();
                    // Do not silently substitute illustrative data; keep failure visible
                }
            });
        }

        private void ToggleTimelineCollapse()
        {
            if (timelineBody == null) return;
            bool isCollapsed = timelineBody.style.display == DisplayStyle.None;
            timelineBody.style.display = isCollapsed ? DisplayStyle.Flex : DisplayStyle.None;
            btnTimelineToggle.text = isCollapsed ? "Hide tracks" : "Show timeline";
            ApplyResponsiveLayout();
        }

        private void UpdateStatusBadge()
        {
            if (statusBadgeText == null) return;
            bool current = assessedData != null && assessedSiteId == app?.SelectedSite?.id &&
                assessedLandingUtc == selectedLandingUtc && Math.Abs(assessedDurationHours-selectedDurationHours) < 1e-9;
            bool loading = missionConfigView?.IsLoading == true;
            bool failed = assessmentState == "Analysis failed";
            statusBadge.style.display = Mode == AppMode.Inspect && (current || loading || failed) ? DisplayStyle.Flex : DisplayStyle.None;
            statusBadge.tooltip = "";
            statusBadge.RemoveFromClassList("status-badge-illustrative");
            statusBadge.RemoveFromClassList("status-badge-geometric");
            statusBadge.RemoveFromClassList("status-badge-terrain");
            if (loading || failed)
            {
                statusBadgeText.text = loading ? "Analyzing mission…" : "Analysis failed — see details";
                statusBadge.AddToClassList("status-badge-illustrative");
                return;
            }
            if (!current) return;
            string status = assessedData.data_status;
            if (status == "INVALID_DATA" || AnalysisRepository.Instance.Sites.Count == 0)
            {
                statusBadgeText.text = "Data rejected";
                statusBadge.AddToClassList("status-badge-illustrative");
                statusBadge.tooltip = "Scientific data failed contract validation.";
                return;
            }

            if (status == "TERRAIN_AWARE_ESTIMATE")
            {
                statusBadgeText.text = "Terrain-aware estimate";
                statusBadge.AddToClassList("status-badge-terrain");
                statusBadge.tooltip = "Real JPL Horizons ephemeris + LOLA DEM terrain horizon profile.";
            }
            else if (status == "GEOMETRIC_ONLY")
            {
                statusBadgeText.text = "NASA / Geometric horizon";
                statusBadge.AddToClassList("status-badge-geometric");
                statusBadge.tooltip = "Real JPL Horizons ephemeris + geometric spherical horizon.";
            }
            else
            {
                statusBadgeText.text = "Illustrative demo";
                statusBadge.AddToClassList("status-badge-illustrative");
                statusBadge.tooltip = "Synthetic demonstrative tracks and harmonic terrain.";
            }
        }

        public bool IsPointerOverUI(Vector2 screen)
        {
            if(Time.unscaledTime<ignoreWorldPickUntil)return true;
            if (!InterfaceVisible) return false;
            var panel = root?.panel;
            if (panel == null) return false;

            if (popupScrim != null && popupScrim.style.display != DisplayStyle.None) return true;
            Vector2 p = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, Screen.height - screen.y));
            var picked = panel.Pick(p);
            if (picked == null) return false;

            return picked != root && picked != overlay && picked != markerLayer;
        }

        public bool PickMarker(Vector2 screen, bool focus)
        {
            if (CurrentPopup != "") { Close(); return true; }
            if (!InterfaceVisible || root?.panel == null) return false;
            var p = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screen.x, Screen.height - screen.y));
            LunarSite chosen = null;

            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i].style.display == DisplayStyle.None) continue;
                // Labels are UI buttons. Only the actual visible 6 px dot may
                // intercept a surface raycast; proximity is never a site selection.
                if (markerDots[i].worldBound.Contains(p)) { chosen = app.sites[i]; break; }
            }

            if (chosen)
            {
                app.SelectSite(chosen);
                RunAnalysis();
                if (focus) app.Director.Focus();
            }
            return chosen != null;
        }

        void Update()
        {
            if (!app) return;
            EnsureUIToolkitInput();
            var document = GetComponent<UIDocument>();
            if (!document.isActiveAndEnabled || document.rootVisualElement == null) return;
            if (root != document.rootVisualElement)
            {
                // Awake is not called again after an Editor domain reload. VisualElements
                // cannot survive serialization, so bind to the new document root here.
                var mode = Mode;
                Initialize(app);
                SetMode(mode);
                if (!InterfaceVisible)
                {
                    InterfaceVisible = true;
                    ToggleInterface();
                }
            }

            // Clock update
            lblTimelineUtc.text = app.Clock.Utc.ToString("yyyy-MM-dd  HH:mm:ss 'UTC'").ToUpperInvariant();
            btnPlayPause.text = app.Clock.paused ? "Play" : "Pause";
            btnPlayPause.tooltip=app.Clock.paused?"Play mission timeline":"Pause mission timeline";

            timelineElement?.SetCurrentTime(app.Clock.seconds);
            skylineChartElement?.SetCurrentTime(app.Clock.seconds);

            // Compute next loss of sun
            var siteData = assessedSiteId == app?.SelectedSite?.id && assessedLandingUtc == selectedLandingUtc &&
                Math.Abs(assessedDurationHours-selectedDurationHours) < 1e-9 ? assessedData : null;
            lblTimelineLossStatus.tooltip = "";
            if (siteData?.visibility?.sun_visible != null && siteData.ephemeris != null)
            {
                int curIdx = Mathf.Clamp((int)Math.Floor(app.Clock.seconds / siteData.ephemeris.step_s), 0, siteData.ephemeris.n - 1);
                int lossSamples = VisibilityMath.NextLoss(siteData.visibility.sun_visible, curIdx);
                if (lossSamples == 0)
                {
                    lblTimelineLossStatus.text = "Sun currently below modeled horizon";
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

            if (siteData == null)
            {
                lblTimelineLossStatus.text = "TIMELINE IDLE — ANALYZE TO LOAD";
                lblTimelineLossStatus.tooltip = "No mission timeline loaded — click Analyze Mission Window to evaluate.";
            }
            // Surface mode overlay
            bool isSurface = app.Director.Mode == CameraMode.Surface;
            surfaceOverlay.style.display = isSurface ? DisplayStyle.Flex : DisplayStyle.None;
            if (isSurface && siteData?.ephemeris == null)
            {
                lblSurfaceSunReadout.text = "Sun direction illustrative - analyze selected location first";
                lblSurfaceEarthReadout.text = "Earth direction illustrative - no live analysis";
            }
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
            float lastMarkerTop=-1000;
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
                    float desiredTop=Mathf.Clamp(panel.y-100,140,root.resolvedStyle.height-350)+i*32;
                    markers[i].style.top=Mathf.Max(desiredTop,lastMarkerTop+32);lastMarkerTop=markers[i].style.top.value.value;
                    float side=root.layout.width<1250?290:root.layout.width<1500?310:340;
                    markers[i].style.left=Mathf.Clamp(panel.x+14,side+32,Mathf.Max(side+32,root.layout.width-side-200));
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

            float minPinX = (inspectorVisible && floatingInspector != null && floatingInspector.style.display != DisplayStyle.None) ? 352f : 16f;
            float maxPinX = (metricsVisible && floatingMetrics != null && floatingMetrics.style.display != DisplayStyle.None) ? root.resolvedStyle.width - 360f : root.resolvedStyle.width - 150f;

            if (earthMarker != null && app?.World?.earth != null)
            {
                var ed = app.Clock.Provider.EarthDirection(app.Clock.Utc);
                var ep = director.view.transform.position + ed * 14000;
                var esp = director.view.WorldToScreenPoint(ep);
                bool earthOnScreen = esp.z > 0 && esp.x >= 0 && esp.x <= Screen.width && esp.y >= 0 && esp.y <= Screen.height;
                bool earthVis = InterfaceVisible && app.World.markersVisible && earthOnScreen;
                earthMarker.style.display = earthVis ? DisplayStyle.Flex : DisplayStyle.None;
                earthMarkerDot.style.display = earthVis ? DisplayStyle.Flex : DisplayStyle.None;
                if (earthVis && root?.panel != null)
                {
                    Vector2 ePanel = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(esp.x, Screen.height - esp.y));
                    earthMarker.style.left = Mathf.Clamp(ePanel.x + 12, minPinX, maxPinX);
                    earthMarker.style.top = Mathf.Clamp(ePanel.y - 14, 80, root.resolvedStyle.height - 180);
                    earthMarkerDot.style.left = ePanel.x - 3;
                    earthMarkerDot.style.top = ePanel.y - 3;
                }
            }

            if (sunMarker != null && app?.World?.sun != null)
            {
                var sd = app.Clock.Provider.SunDirection(app.Clock.Utc);
                var sp = director.view.transform.position + sd * 14000;
                var ssp = director.view.WorldToScreenPoint(sp);
                bool sunOnScreen = ssp.z > 0 && ssp.x >= 0 && ssp.x <= Screen.width && ssp.y >= 0 && ssp.y <= Screen.height;
                bool sunVis = InterfaceVisible && app.World.markersVisible && sunOnScreen;
                sunMarker.style.display = sunVis ? DisplayStyle.Flex : DisplayStyle.None;
                sunMarkerDot.style.display = sunVis ? DisplayStyle.Flex : DisplayStyle.None;
                if (sunVis && root?.panel != null)
                {
                    Vector2 sPanel = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(ssp.x, Screen.height - ssp.y));
                    sunMarker.style.left = Mathf.Clamp(sPanel.x + 12, minPinX, maxPinX);
                    sunMarker.style.top = Mathf.Clamp(sPanel.y - 14, 80, root.resolvedStyle.height - 180);
                    sunMarkerDot.style.left = sPanel.x - 3;
                    sunMarkerDot.style.top = sPanel.y - 3;
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
