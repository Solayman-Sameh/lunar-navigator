using System;
using System.Collections.Generic;
using System.Globalization;
using LunarNavigator.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace LunarNavigator.UI
{
    public class OpportunityFinderView : VisualElement
    {
        // UI Elements - Inputs
        public TextField TxtEarliestUtc { get; private set; }
        public TextField TxtSearchWindowH { get; private set; }
        public TextField TxtMissionDurationH { get; private set; }

        // Candidate Sites
        public Button ChipSiteConnecting { get; private set; }
        public Button ChipSiteMalapert { get; private set; }
        public Button ChipSiteShackleton { get; private set; }
        private readonly HashSet<string> selectedSiteIds = new HashSet<string>
        {
            "connecting_ridge",
            "malapert_mountain",
            "shackleton_rim"
        };

        // Constraints Sliders
        public Slider SliderMinSun { get; private set; }
        public Slider SliderMinEarth { get; private set; }
        public Slider SliderMaxDark { get; private set; }
        public Slider SliderMaxComm { get; private set; }

        public Label LblMinSunVal { get; private set; }
        public Label LblMinEarthVal { get; private set; }
        public Label LblMaxDarkVal { get; private set; }
        public Label LblMaxCommVal { get; private set; }

        // Ranking Policy
        public Button BtnPolicyBest { get; private set; }
        public Button BtnPolicyEarliest { get; private set; }
        public string SelectedPolicy { get; private set; } = "BEST_OVERALL";

        // Action Buttons
        public Button BtnSearch { get; private set; }
        public Button BtnClose { get; private set; }

        // Status & Errors
        public VisualElement LoadingIndicator { get; private set; }
        public Label LblLoadingStatus { get; private set; }
        public Button BtnCancel { get; private set; }

        public VisualElement ErrorBanner { get; private set; }
        public Label LblErrorMessage { get; private set; }

        // Results Container
        public VisualElement ResultsScrollView { get; private set; }

        // State
        public DateTime SelectedEarliestUtc { get; private set; } = DemoSimulation.DeviceUtcSecond;
        public double SelectedSearchWindowH { get; private set; } = 48.0;
        public double SelectedMissionDurationH { get; private set; } = 24.0;
        public bool IsLoading { get; private set; } = false;

        // Aerospace Segmented Stepper Labels
        private Label lblYearVal;
        private Label lblMonthVal;
        private Label lblDayVal;
        private Label lblHourVal;
        private Label lblMinuteVal;
        private Label lblSecondVal;

        // Segmented Duration Stepper Labels
        private Label lblSearchDaysVal;
        private Label lblSearchHoursVal;
        private Label lblSearchTotalH;

        private Label lblStayDaysVal;
        private Label lblStayHoursVal;
        private Label lblStayTotalH;

        // Duration Chips
        private Button chipSpan24;
        private Button chipSpan48;
        private Button chipSpan72;
        private Button chipSpan168;
        private Button chipSpan336;

        private Button chipDur12;
        private Button chipDur24;
        private Button chipDur48;
        private Button chipDur72;
        private Button chipDur168;

        private Label lblWindowSummary;

        // Events
        public event Action<LiveOpportunityRequest> OnSearchRequested;
        public event Action OnCancelRequested;
        public event Action<LiveOpportunityRecommendation> OnRecommendationSelected;
        public event Action OnCloseRequested;

        public OpportunityFinderView()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            Clear();
            style.flexDirection = FlexDirection.Column;

            // 1. Header
            var head = new VisualElement();
            head.AddToClassList("card-header");
            var title = new Label("OPPORTUNITY FINDER");
            title.AddToClassList("card-title");
            head.Add(title);

            BtnClose = new Button(() => OnCloseRequested?.Invoke()) { text = "×" };
            BtnClose.AddToClassList("icon-btn");
            head.Add(BtnClose);
            Add(head);

            var sub = new Label("Live NASA Horizons Search · GEOMETRIC_ONLY");
            sub.AddToClassList("subtle");
            sub.style.marginBottom = 6;
            sub.style.whiteSpace = WhiteSpace.Normal;
            sub.text = "Search NASA reference sites for landing windows that satisfy your mission requirements. Custom picks are not included.";
            Add(sub);

            // 2. Earliest Landing Date/Time (Aerospace Segmented Datetime Stepper)
            var timeRow = new VisualElement();
            timeRow.AddToClassList("field-row");
            timeRow.style.marginTop = 6;

            var lblEarliest = new Label("EARLIEST CANDIDATE ARRIVAL (UTC)");
            lblEarliest.AddToClassList("field-label");
            timeRow.Add(lblEarliest);

            TxtEarliestUtc = new TextField();
            TxtEarliestUtc.style.display = DisplayStyle.None;
            TxtEarliestUtc.value = SelectedEarliestUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";
            timeRow.Add(TxtEarliestUtc);

            var stepperRow = new VisualElement();
            stepperRow.AddToClassList("datetime-stepper-row");

            lblYearVal = new Label(SelectedEarliestUtc.Year.ToString("D4"));
            lblMonthVal = new Label(SelectedEarliestUtc.Month.ToString("D2"));
            lblDayVal = new Label(SelectedEarliestUtc.Day.ToString("D2"));
            lblHourVal = new Label(SelectedEarliestUtc.Hour.ToString("D2"));
            lblMinuteVal = new Label(SelectedEarliestUtc.Minute.ToString("D2"));
            lblSecondVal = new Label(SelectedEarliestUtc.Second.ToString("D2"));

            stepperRow.Add(CreateStepperUnit("year", lblYearVal, () => StepYear(1), () => StepYear(-1), 46f, "Step Year (±1y) · Use arrow buttons"));
            var sepYM = new Label("-") { name = "sep-ym" };
            sepYM.AddToClassList("stepper-separator");
            stepperRow.Add(sepYM);

            stepperRow.Add(CreateStepperUnit("month", lblMonthVal, () => StepMonth(1), () => StepMonth(-1), 30f, "Step Month (±1m) · Use arrow buttons"));
            var sepMD = new Label("-") { name = "sep-md" };
            sepMD.AddToClassList("stepper-separator");
            stepperRow.Add(sepMD);

            stepperRow.Add(CreateStepperUnit("day", lblDayVal, () => StepDay(1), () => StepDay(-1), 30f, "Step Day (±1d) · Use arrow buttons"));

            var spaceGap = new VisualElement();
            spaceGap.style.width = 6;
            stepperRow.Add(spaceGap);

            stepperRow.Add(CreateStepperUnit("hour", lblHourVal, () => StepHour(1), () => StepHour(-1), 30f, "Step Hour (±1h) · Use arrow buttons"));
            var sepHM = new Label(":") { name = "sep-hm" };
            sepHM.AddToClassList("stepper-separator");
            stepperRow.Add(sepHM);

            stepperRow.Add(CreateStepperUnit("minute", lblMinuteVal, () => StepMinute(10), () => StepMinute(-10), 30f, "Step Minute (±10min) · Use arrow buttons"));

            var sepMS = new Label(":"); sepMS.AddToClassList("stepper-separator"); stepperRow.Add(sepMS);
            stepperRow.Add(CreateStepperUnit("second", lblSecondVal, () => StepSecond(1), () => StepSecond(-1), 30f, "Seconds (+/-1)"));
            var utcTag = new Label("UTC");
            utcTag.AddToClassList("stepper-utc-tag");
            utcTag.tooltip = "Coordinated Universal Time";
            stepperRow.Add(utcTag);

            timeRow.Add(stepperRow);
            var useNow = new Button(() => SetEarliestUtc(DemoSimulation.DeviceUtcSecond)) {
                text = "Use current UTC", name = "finder-use-current-utc",
                tooltip = "Start the search from this device's current UTC clock, including seconds."
            };
            useNow.AddToClassList("chip-btn");
            timeRow.Add(useNow);
            Add(timeRow);

            // 3. Durations (Search Window & Surface Stay vertically stacked with Days & Hours Segmented Steppers)

            // Search Window Section
            var secSearch = new VisualElement();
            secSearch.AddToClassList("field-row");
            secSearch.style.marginTop = 6;

            var lblWin = new Label("SEARCH WINDOW (LOOKAHEAD)");
            lblWin.AddToClassList("field-label");
            lblWin.tooltip = "How far ahead from Earliest Arrival to scan for candidate landing opportunities.";
            secSearch.Add(lblWin);

            TxtSearchWindowH = new TextField { value = SelectedSearchWindowH.ToString("F1", CultureInfo.InvariantCulture) };
            TxtSearchWindowH.style.display = DisplayStyle.None;
            secSearch.Add(TxtSearchWindowH);

            var searchStepperRow = new VisualElement();
            searchStepperRow.AddToClassList("datetime-stepper-row");
            searchStepperRow.AddToClassList("duration-stepper-row");
            searchStepperRow.style.alignItems = Align.Center;

            int initWinDays = (int)(SelectedSearchWindowH / 24);
            int initWinHours = (int)(SelectedSearchWindowH % 24);
            lblSearchDaysVal = new Label(initWinDays.ToString("D2"));
            lblSearchHoursVal = new Label(initWinHours.ToString("D2"));
            lblSearchTotalH = new Label($"({SelectedSearchWindowH:0.#} h total)");
            lblSearchTotalH.AddToClassList("stepper-total-tag");
            lblSearchTotalH.tooltip = "Total duration in cumulative hours";

            searchStepperRow.Add(CreateStepperUnit("search-days", lblSearchDaysVal, () => StepSearchDays(1), () => StepSearchDays(-1), 38f, "Step Days (±1d) · Use arrow buttons"));
            var lblSearchDaysUnit = new Label("d");
            lblSearchDaysUnit.AddToClassList("stepper-unit-label");
            lblSearchDaysUnit.tooltip = "Days";
            searchStepperRow.Add(lblSearchDaysUnit);

            var gapSearch = new VisualElement { style = { width = 8 } };
            searchStepperRow.Add(gapSearch);

            searchStepperRow.Add(CreateStepperUnit("search-hours", lblSearchHoursVal, () => StepSearchHours(1), () => StepSearchHours(-1), 38f, "Step Hours (±1h) · Use arrow buttons"));
            var lblSearchHoursUnit = new Label("h");
            lblSearchHoursUnit.AddToClassList("stepper-unit-label");
            lblSearchHoursUnit.tooltip = "Hours";
            searchStepperRow.Add(lblSearchHoursUnit);

            searchStepperRow.Add(lblSearchTotalH);
            secSearch.Add(searchStepperRow);

            var spanChipsRow = new VisualElement();
            spanChipsRow.AddToClassList("chip-row-symmetrical");
            spanChipsRow.style.marginTop = 4;
            chipSpan24 = new Button(() => SetSearchSpan(24.0)) { text = "1 day", tooltip = "24 hours (1 day)" }; chipSpan24.AddToClassList("chip-btn"); chipSpan24.AddToClassList("chip-btn-sm"); spanChipsRow.Add(chipSpan24);
            chipSpan48 = new Button(() => SetSearchSpan(48.0)) { text = "2 days", tooltip = "48 hours (2 days)" }; chipSpan48.AddToClassList("chip-btn"); chipSpan48.AddToClassList("chip-btn-sm"); spanChipsRow.Add(chipSpan48);
            chipSpan72 = new Button(() => SetSearchSpan(72.0)) { text = "3 days", tooltip = "72 hours (3 days)" }; chipSpan72.AddToClassList("chip-btn"); chipSpan72.AddToClassList("chip-btn-sm"); spanChipsRow.Add(chipSpan72);
            chipSpan168 = new Button(() => SetSearchSpan(168.0)) { text = "7 days", tooltip = "168 hours (7 days)" }; chipSpan168.AddToClassList("chip-btn"); chipSpan168.AddToClassList("chip-btn-sm"); spanChipsRow.Add(chipSpan168);
            chipSpan336 = new Button(() => SetSearchSpan(336.0)) { text = "14 days", tooltip = "336 hours (14 days)" }; chipSpan336.AddToClassList("chip-btn"); chipSpan336.AddToClassList("chip-btn-sm"); spanChipsRow.Add(chipSpan336);
            secSearch.Add(spanChipsRow);
            Add(secSearch);

            // Surface Stay Section
            var secStay = new VisualElement();
            secStay.AddToClassList("field-row");
            secStay.style.marginTop = 8;

            var lblMis = new Label("SURFACE STAY (REQUIRED)");
            lblMis.AddToClassList("field-label");
            lblMis.tooltip = "Required operating duration for the lander on the lunar surface.";
            secStay.Add(lblMis);

            TxtMissionDurationH = new TextField { value = SelectedMissionDurationH.ToString("F1", CultureInfo.InvariantCulture) };
            TxtMissionDurationH.style.display = DisplayStyle.None;
            secStay.Add(TxtMissionDurationH);

            var stayStepperRow = new VisualElement();
            stayStepperRow.AddToClassList("datetime-stepper-row");
            stayStepperRow.AddToClassList("duration-stepper-row");
            stayStepperRow.style.alignItems = Align.Center;

            int initStayDays = (int)(SelectedMissionDurationH / 24);
            int initStayHours = (int)(SelectedMissionDurationH % 24);
            lblStayDaysVal = new Label(initStayDays.ToString("D2"));
            lblStayHoursVal = new Label(initStayHours.ToString("D2"));
            lblStayTotalH = new Label($"({SelectedMissionDurationH:0.#} h total)");
            lblStayTotalH.AddToClassList("stepper-total-tag");
            lblStayTotalH.tooltip = "Total duration in cumulative hours";

            stayStepperRow.Add(CreateStepperUnit("stay-days", lblStayDaysVal, () => StepStayDays(1), () => StepStayDays(-1), 38f, "Step Days (±1d) · Use arrow buttons"));
            var lblStayDaysUnit = new Label("d");
            lblStayDaysUnit.AddToClassList("stepper-unit-label");
            lblStayDaysUnit.tooltip = "Days";
            stayStepperRow.Add(lblStayDaysUnit);

            var gapStay = new VisualElement { style = { width = 8 } };
            stayStepperRow.Add(gapStay);

            stayStepperRow.Add(CreateStepperUnit("stay-hours", lblStayHoursVal, () => StepStayHours(1), () => StepStayHours(-1), 38f, "Step Hours (±1h) · Use arrow buttons"));
            var lblStayHoursUnit = new Label("h");
            lblStayHoursUnit.AddToClassList("stepper-unit-label");
            lblStayHoursUnit.tooltip = "Hours";
            stayStepperRow.Add(lblStayHoursUnit);

            stayStepperRow.Add(lblStayTotalH);
            secStay.Add(stayStepperRow);

            var stayChipsRow = new VisualElement();
            stayChipsRow.AddToClassList("chip-row-symmetrical");
            stayChipsRow.style.marginTop = 4;
            chipDur12 = new Button(() => SetMissionDuration(12.0)) { text = "12 hrs", tooltip = "12 hours (0.5 day)" }; chipDur12.AddToClassList("chip-btn"); chipDur12.AddToClassList("chip-btn-sm"); stayChipsRow.Add(chipDur12);
            chipDur24 = new Button(() => SetMissionDuration(24.0)) { text = "1 day", tooltip = "24 hours (1 day)" }; chipDur24.AddToClassList("chip-btn"); chipDur24.AddToClassList("chip-btn-sm"); stayChipsRow.Add(chipDur24);
            chipDur48 = new Button(() => SetMissionDuration(48.0)) { text = "2 days", tooltip = "48 hours (2 days)" }; chipDur48.AddToClassList("chip-btn"); chipDur48.AddToClassList("chip-btn-sm"); stayChipsRow.Add(chipDur48);
            chipDur72 = new Button(() => SetMissionDuration(72.0)) { text = "3 days", tooltip = "72 hours (3 days)" }; chipDur72.AddToClassList("chip-btn"); chipDur72.AddToClassList("chip-btn-sm"); stayChipsRow.Add(chipDur72);
            chipDur168 = new Button(() => SetMissionDuration(168.0)) { text = "7 days", tooltip = "168 hours (7 days)" }; chipDur168.AddToClassList("chip-btn"); chipDur168.AddToClassList("chip-btn-sm"); stayChipsRow.Add(chipDur168);
            secStay.Add(stayChipsRow);
            Add(secStay);

            // Explanatory Micro-Copy Line
            lblWindowSummary = new Label();
            lblWindowSummary.AddToClassList("subtle");
            lblWindowSummary.style.fontSize = 10;
            lblWindowSummary.style.color = new Color(0.65f, 0.8f, 0.95f);
            lblWindowSummary.style.whiteSpace = WhiteSpace.Normal;
            lblWindowSummary.style.marginTop = 6;
            lblWindowSummary.style.marginBottom = 6;
            UpdateWindowSummary();
            Add(lblWindowSummary);

            UpdateSpanChips();
            UpdateDurationChips();

            // 4. Candidate Sites Chips
            Add(CreateSectionHeaderWithInfo(
                "CANDIDATE SITES", 
                "Select which NASA South-Pole landing sites to evaluate in this search (click to include/exclude).", 
                "info-candidate-sites"));

            var siteChipsRow = new VisualElement();
            siteChipsRow.AddToClassList("chip-row");
            siteChipsRow.style.flexWrap = Wrap.Wrap;

            ChipSiteConnecting = new Button(() => ToggleSite("connecting_ridge", ChipSiteConnecting)) { text = "Connecting Ridge", tooltip = "Connecting Ridge reference center (-89.44° S, 222.65° E)" };
            ChipSiteConnecting.AddToClassList("chip-btn");
            ChipSiteConnecting.AddToClassList("chip-btn-active");
            ChipSiteConnecting.style.marginBottom = 4;
            siteChipsRow.Add(ChipSiteConnecting);

            ChipSiteMalapert = new Button(() => ToggleSite("malapert_mountain", ChipSiteMalapert)) { text = "Malapert Massif", tooltip = "Malapert Massif reference center (-85.99° S, 357.76° E)" };
            ChipSiteMalapert.AddToClassList("chip-btn");
            ChipSiteMalapert.AddToClassList("chip-btn-active");
            ChipSiteMalapert.style.marginBottom = 4;
            siteChipsRow.Add(ChipSiteMalapert);

            ChipSiteShackleton = new Button(() => ToggleSite("shackleton_rim", ChipSiteShackleton)) { text = "Shackleton Rim", tooltip = "Shackleton Rim reference center (-89.66° S, 129.20° E)" };
            ChipSiteShackleton.AddToClassList("chip-btn");
            ChipSiteShackleton.AddToClassList("chip-btn-active");
            ChipSiteShackleton.style.marginBottom = 4;
            siteChipsRow.Add(ChipSiteShackleton);
            Add(siteChipsRow);

            // 5. Constraints
            Add(CreateSectionHeaderWithInfo(
                "HARD CONSTRAINTS (ALL MUST PASS)", 
                "All four conditions must be satisfied for a landing window to qualify.", 
                "info-constraints"));

            // Min Sun Slider
            var rSun = new VisualElement();
            rSun.AddToClassList("row");
            rSun.style.justifyContent = Justify.SpaceBetween;
            var lSun = new Label("Min Sun-visible:"); lSun.AddToClassList("subtle"); rSun.Add(lSun);
            LblMinSunVal = new Label("30%"); LblMinSunVal.AddToClassList("subtle"); rSun.Add(LblMinSunVal);
            Add(rSun);
            SliderMinSun = new Slider(0f, 100f) { value = 30f };
            SliderMinSun.RegisterValueChangedCallback(evt => LblMinSunVal.text = $"{evt.newValue:F0}%");
            Add(SliderMinSun);

            // Min Earth Slider
            var rEarth = new VisualElement();
            rEarth.AddToClassList("row");
            rEarth.style.justifyContent = Justify.SpaceBetween;
            var lEarth = new Label("Min Earth-visible:"); lEarth.AddToClassList("subtle"); rEarth.Add(lEarth);
            LblMinEarthVal = new Label("40%"); LblMinEarthVal.AddToClassList("subtle"); rEarth.Add(LblMinEarthVal);
            Add(rEarth);
            SliderMinEarth = new Slider(0f, 100f) { value = 40f };
            SliderMinEarth.RegisterValueChangedCallback(evt => LblMinEarthVal.text = $"{evt.newValue:F0}%");
            Add(SliderMinEarth);

            // Max Dark Slider
            var rDark = new VisualElement();
            rDark.AddToClassList("row");
            rDark.style.justifyContent = Justify.SpaceBetween;
            var lDark = new Label("Max Continuous Dark:"); lDark.AddToClassList("subtle"); rDark.Add(lDark);
            LblMaxDarkVal = new Label("36h"); LblMaxDarkVal.AddToClassList("subtle"); rDark.Add(LblMaxDarkVal);
            Add(rDark);
            SliderMaxDark = new Slider(0f, 72f) { value = 36f };
            SliderMaxDark.RegisterValueChangedCallback(evt => LblMaxDarkVal.text = $"{evt.newValue:F0}h");
            Add(SliderMaxDark);

            // Max Comm Gap Slider
            var rComm = new VisualElement();
            rComm.AddToClassList("row");
            rComm.style.justifyContent = Justify.SpaceBetween;
            var lComm = new Label("Max Comms Outage:"); lComm.AddToClassList("subtle"); rComm.Add(lComm);
            LblMaxCommVal = new Label("48h"); LblMaxCommVal.AddToClassList("subtle"); rComm.Add(LblMaxCommVal);
            Add(rComm);
            SliderMaxComm = new Slider(0f, 72f) { value = 48f };
            SliderMaxComm.RegisterValueChangedCallback(evt => LblMaxCommVal.text = $"{evt.newValue:F0}h");
            Add(SliderMaxComm);

            // 6. Ranking Policy Chips
            Add(CreateSectionHeaderWithInfo(
                "RANKING POLICY", 
                "Best Overall: Ranks by highest sunlight & Earth comms quality.\nEarliest Qualifying: Ranks by soonest date meeting your minimum requirements.", 
                "info-policy"));

            var polRow = new VisualElement();
            polRow.AddToClassList("chip-row");
            BtnPolicyBest = new Button(() => SetPolicy("BEST_OVERALL")) { text = "Best Overall" };
            BtnPolicyBest.AddToClassList("chip-btn");
            BtnPolicyBest.AddToClassList("chip-btn-active");
            polRow.Add(BtnPolicyBest);

            BtnPolicyEarliest = new Button(() => SetPolicy("EARLIEST_QUALIFYING")) { text = "Earliest Qualifying" };
            BtnPolicyEarliest.AddToClassList("chip-btn");
            polRow.Add(BtnPolicyEarliest);
            Add(polRow);

            // 7. Search Action Button
            BtnSearch = new Button(TriggerSearch) { text = "FIND OPPORTUNITIES" };
            BtnSearch.AddToClassList("btn-primary");
            BtnSearch.style.width = Length.Percent(100);
            BtnSearch.style.marginBottom = 6;
            Add(BtnSearch);

            // 8. Loading Box (hidden by default)
            LoadingIndicator = new VisualElement { name = "loading-box" };
            LoadingIndicator.AddToClassList("mission-loading-box");
            LoadingIndicator.style.display = DisplayStyle.None;
            LblLoadingStatus = new Label("Searching NASA opportunities...");
            LblLoadingStatus.style.color = new Color(0.35f, 0.66f, 0.96f);
            LblLoadingStatus.style.fontSize = 11;
            LoadingIndicator.Add(LblLoadingStatus);

            BtnCancel = new Button(() => OnCancelRequested?.Invoke()) { text = "Cancel" };
            BtnCancel.AddToClassList("btn-cancel-small");
            LoadingIndicator.Add(BtnCancel);
            Add(LoadingIndicator);

            // 9. Error Banner (hidden by default)
            ErrorBanner = new VisualElement { name = "error-banner" };
            ErrorBanner.AddToClassList("mission-error-banner");
            ErrorBanner.style.display = DisplayStyle.None;
            LblErrorMessage = new Label();
            LblErrorMessage.style.color = new Color(1f, 0.45f, 0.45f);
            LblErrorMessage.style.fontSize = 10;
            LblErrorMessage.style.whiteSpace = WhiteSpace.Normal;
            ErrorBanner.Add(LblErrorMessage);
            Add(ErrorBanner);

            // 10. Results ScrollView
            ResultsScrollView = new VisualElement { name = "results-scroll" };
            ResultsScrollView.style.flexShrink = 0;
            ResultsScrollView.style.marginTop = 6;
            Add(ResultsScrollView);
            foreach(var field in new[]{TxtEarliestUtc,TxtSearchWindowH,TxtMissionDurationH}) MissionConfigView.ConfigureInput(field);
        }

        private void ToggleSite(string siteId, Button btn)
        {
            if (selectedSiteIds.Contains(siteId))
            {
                if (selectedSiteIds.Count > 1) // Keep at least one
                {
                    selectedSiteIds.Remove(siteId);
                    btn.RemoveFromClassList("chip-btn-active");
                }
            }
            else
            {
                if (selectedSiteIds.Count < 3)
                {
                    selectedSiteIds.Add(siteId);
                    btn.AddToClassList("chip-btn-active");
                }
            }
        }

        public void SetPolicy(string policy)
        {
            SelectedPolicy = policy;
            BtnPolicyBest.EnableInClassList("chip-btn-active", policy == "BEST_OVERALL");
            BtnPolicyEarliest.EnableInClassList("chip-btn-active", policy == "EARLIEST_QUALIFYING");
        }

        private VisualElement CreateSectionHeaderWithInfo(string title, string infoTooltip, string infoId)
        {
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginTop = 6;
            headerRow.style.marginBottom = 2;

            var lbl = new Label(title);
            lbl.AddToClassList("field-label");
            lbl.style.marginBottom = 0;
            headerRow.Add(lbl);

            var infoBtn = new Button { text = "i", name = infoId, tooltip = infoTooltip, focusable = true };
            infoBtn.AddToClassList("assessment-info");
            headerRow.Add(infoBtn);

            return headerRow;
        }

        private VisualElement CreateStepperUnit(string name, Label valLabel, Action onUp, Action onDown, float width, string tooltip)
        {
            var unit = new VisualElement { name = "unit-" + name };
            unit.AddToClassList("stepper-unit");
            unit.style.width = width;
            unit.tooltip = tooltip;

            var upBtn = new Button(onUp) { text = "▲" };
            upBtn.AddToClassList("stepper-arrow-btn");
            upBtn.tooltip = "Increase (" + name + ")";
            unit.Add(upBtn);

            valLabel.name = "val-" + name;
            valLabel.AddToClassList("stepper-digit-box");
            unit.Add(valLabel);

            var downBtn = new Button(onDown) { text = "▼" };
            downBtn.AddToClassList("stepper-arrow-btn");
            downBtn.tooltip = "Decrease (" + name + ")";
            unit.Add(downBtn);

            // The wheel belongs to the containing form; use arrows to edit values.
            return unit;
        }

        public void StepYear(int delta)
        {
            try { SelectedEarliestUtc = SelectedEarliestUtc.AddYears(delta); } catch {}
            UpdateStepperDisplay();
        }

        public void StepMonth(int delta)
        {
            try { SelectedEarliestUtc = SelectedEarliestUtc.AddMonths(delta); } catch {}
            UpdateStepperDisplay();
        }

        public void StepDay(int delta)
        {
            try { SelectedEarliestUtc = SelectedEarliestUtc.AddDays(delta); } catch {}
            UpdateStepperDisplay();
        }

        public void StepHour(int delta)
        {
            try { SelectedEarliestUtc = SelectedEarliestUtc.AddHours(delta); } catch {}
            UpdateStepperDisplay();
        }

        public void StepMinute(int delta)
        {
            try { SelectedEarliestUtc = SelectedEarliestUtc.AddMinutes(delta); } catch {}
            UpdateStepperDisplay();
        }

        private void UpdateStepperDisplay()
        {
            if (lblYearVal != null) lblYearVal.text = SelectedEarliestUtc.Year.ToString("D4");
            if (lblMonthVal != null) lblMonthVal.text = SelectedEarliestUtc.Month.ToString("D2");
            if (lblDayVal != null) lblDayVal.text = SelectedEarliestUtc.Day.ToString("D2");
            if (lblHourVal != null) lblHourVal.text = SelectedEarliestUtc.Hour.ToString("D2");
            if (lblMinuteVal != null) lblMinuteVal.text = SelectedEarliestUtc.Minute.ToString("D2");
            if (lblSecondVal != null) lblSecondVal.text = SelectedEarliestUtc.Second.ToString("D2");

            if (TxtEarliestUtc != null)
            {
                TxtEarliestUtc.value = SelectedEarliestUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";
            }
            ClearError();
        }

        public void StepSecond(int delta)
        {
            try { SelectedEarliestUtc = SelectedEarliestUtc.AddSeconds(delta); } catch (ArgumentOutOfRangeException) { }
            UpdateStepperDisplay();
        }

        private void UpdateWindowSummary()
        {
            if (lblWindowSummary == null) return;
            int winDays = (int)(SelectedSearchWindowH / 24);
            int winHours = (int)(SelectedSearchWindowH % 24);
            string winText = winDays > 0
                ? (winHours > 0 ? $"{winDays} d {winHours} h" : $"{winDays} day{(winDays > 1 ? "s" : "")}")
                : $"{winHours} h";

            int stayDays = (int)(SelectedMissionDurationH / 24);
            int stayHours = (int)(SelectedMissionDurationH % 24);
            string stayText = stayDays > 0
                ? (stayHours > 0 ? $"{stayDays} d {stayHours} h" : $"{stayDays} day{(stayDays > 1 ? "s" : "")}")
                : $"{stayHours} h";

            lblWindowSummary.text = $"Searching candidate landings across next {winText} ({SelectedSearchWindowH:0.#} h) for a {stayText} ({SelectedMissionDurationH:0.#} h) surface stay.";
        }

        private void UpdateSearchSpanDisplay()
        {
            int days = (int)(SelectedSearchWindowH / 24);
            int hours = (int)(SelectedSearchWindowH % 24);
            if (lblSearchDaysVal != null) lblSearchDaysVal.text = days.ToString("D2");
            if (lblSearchHoursVal != null) lblSearchHoursVal.text = hours.ToString("D2");
            if (lblSearchTotalH != null) lblSearchTotalH.text = $"({SelectedSearchWindowH:0.#} h total)";
        }

        private void UpdateMissionDurationDisplay()
        {
            int days = (int)(SelectedMissionDurationH / 24);
            int hours = (int)(SelectedMissionDurationH % 24);
            if (lblStayDaysVal != null) lblStayDaysVal.text = days.ToString("D2");
            if (lblStayHoursVal != null) lblStayHoursVal.text = hours.ToString("D2");
            if (lblStayTotalH != null) lblStayTotalH.text = $"({SelectedMissionDurationH:0.#} h total)";
        }

        public void SetSearchSpan(double hours)
        {
            SelectedSearchWindowH = Math.Clamp(hours, 6.0, 744.0);
            if (TxtSearchWindowH != null)
                TxtSearchWindowH.value = SelectedSearchWindowH.ToString("F1", CultureInfo.InvariantCulture);
            UpdateSearchSpanDisplay();
            UpdateSpanChips();
            UpdateWindowSummary();
            ClearError();
        }

        public void SetMissionDuration(double hours)
        {
            SelectedMissionDurationH = Math.Clamp(hours, 1.0, 744.0);
            if (TxtMissionDurationH != null)
                TxtMissionDurationH.value = SelectedMissionDurationH.ToString("F1", CultureInfo.InvariantCulture);
            UpdateMissionDurationDisplay();
            UpdateDurationChips();
            UpdateWindowSummary();
            ClearError();
        }

        public void StepSearchDays(int delta)
        {
            int currentDays = (int)(SelectedSearchWindowH / 24);
            int currentHours = (int)(SelectedSearchWindowH % 24);
            int newDays = Mathf.Clamp(currentDays + delta, 0, 31);
            double newTotal = newDays * 24 + currentHours;
            if (newTotal < 6) newTotal = 6;
            if (newTotal > 744) newTotal = 744;
            SetSearchSpan(newTotal);
        }

        public void StepSearchHours(int delta)
        {
            double newTotal = SelectedSearchWindowH + delta;
            if (newTotal < 6) newTotal = 6;
            if (newTotal > 744) newTotal = 744;
            SetSearchSpan(newTotal);
        }

        public void StepStayDays(int delta)
        {
            int currentDays = (int)(SelectedMissionDurationH / 24);
            int currentHours = (int)(SelectedMissionDurationH % 24);
            int newDays = Mathf.Clamp(currentDays + delta, 0, 31);
            double newTotal = newDays * 24 + currentHours;
            if (newTotal < 1) newTotal = 1;
            if (newTotal > 744) newTotal = 744;
            SetMissionDuration(newTotal);
        }

        public void StepStayHours(int delta)
        {
            double newTotal = SelectedMissionDurationH + delta;
            if (newTotal < 1) newTotal = 1;
            if (newTotal > 744) newTotal = 744;
            SetMissionDuration(newTotal);
        }

        private void UpdateSpanChips()
        {
            chipSpan24?.EnableInClassList("chip-btn-active", Math.Abs(SelectedSearchWindowH - 24.0) < 0.1);
            chipSpan48?.EnableInClassList("chip-btn-active", Math.Abs(SelectedSearchWindowH - 48.0) < 0.1);
            chipSpan72?.EnableInClassList("chip-btn-active", Math.Abs(SelectedSearchWindowH - 72.0) < 0.1);
            chipSpan168?.EnableInClassList("chip-btn-active", Math.Abs(SelectedSearchWindowH - 168.0) < 0.1);
            chipSpan336?.EnableInClassList("chip-btn-active", Math.Abs(SelectedSearchWindowH - 336.0) < 0.1);
        }

        private void UpdateDurationChips()
        {
            chipDur12?.EnableInClassList("chip-btn-active", Math.Abs(SelectedMissionDurationH - 12.0) < 0.1);
            chipDur24?.EnableInClassList("chip-btn-active", Math.Abs(SelectedMissionDurationH - 24.0) < 0.1);
            chipDur48?.EnableInClassList("chip-btn-active", Math.Abs(SelectedMissionDurationH - 48.0) < 0.1);
            chipDur72?.EnableInClassList("chip-btn-active", Math.Abs(SelectedMissionDurationH - 72.0) < 0.1);
            chipDur168?.EnableInClassList("chip-btn-active", Math.Abs(SelectedMissionDurationH - 168.0) < 0.1);
        }

        private void AdjustEarliestDays(int days)
        {
            StepDay(days);
        }

        private void AdjustEarliestHours(int hours)
        {
            StepHour(hours);
        }

        private void ParseEarliestUtc(string text)
        {
            string clean = text.Replace("UTC", "").Trim();
            if (DateTime.TryParse(clean, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
            {
                SelectedEarliestUtc = dt;
                UpdateStepperDisplay();
            }
        }

        public void SetEarliestUtc(DateTime utc)
        {
            SelectedEarliestUtc = utc.ToUniversalTime();
            UpdateStepperDisplay();
        }

        public void SetSearchParameters(
            DateTime earliestUtc,
            double searchWindowH,
            double missionDurationH,
            string[] candidateSites,
            double minSun,
            double minEarth,
            double maxDark,
            double maxComm,
            string policy)
        {
            SetEarliestUtc(earliestUtc);
            SetSearchSpan(searchWindowH);
            SetMissionDuration(missionDurationH);

            selectedSiteIds.Clear();
            if (candidateSites != null)
            {
                foreach (var s in candidateSites) selectedSiteIds.Add(s);
            }
            ChipSiteConnecting.EnableInClassList("chip-btn-active", selectedSiteIds.Contains("connecting_ridge"));
            ChipSiteMalapert.EnableInClassList("chip-btn-active", selectedSiteIds.Contains("malapert_mountain"));
            ChipSiteShackleton.EnableInClassList("chip-btn-active", selectedSiteIds.Contains("shackleton_rim"));

            SliderMinSun.value = (float)(minSun * 100.0);
            SliderMinEarth.value = (float)(minEarth * 100.0);
            SliderMaxDark.value = (float)maxDark;
            SliderMaxComm.value = (float)maxComm;

            SetPolicy(policy);
        }

        private void TriggerSearch()
        {
            string dateText=TxtEarliestUtc.value.Trim();
            if(dateText.EndsWith(" UTC",StringComparison.OrdinalIgnoreCase)) dateText=dateText.Substring(0,dateText.Length-4);
            if(!DateTime.TryParseExact(dateText,new[]{"yyyy-MM-dd HH:mm:ss","yyyy-MM-dd HH:mm"},CultureInfo.InvariantCulture,DateTimeStyles.AdjustToUniversal|DateTimeStyles.AssumeUniversal,out var earliest) ||
               !double.TryParse(TxtSearchWindowH.value,NumberStyles.Float,CultureInfo.InvariantCulture,out var span) ||
               !double.TryParse(TxtMissionDurationH.value,NumberStyles.Float,CultureInfo.InvariantCulture,out var mission) ||
               double.IsNaN(span) || double.IsInfinity(span) || span<=0 || span>744 ||
               double.IsNaN(mission) || double.IsInfinity(mission) || mission<=0 || mission>744)
            {
                ShowError("Enter a valid UTC date/time and positive search/mission hours (maximum combined 744 h).");
                return;
            }
            SelectedEarliestUtc=earliest.ToUniversalTime();
            SelectedSearchWindowH=span;SelectedMissionDurationH=mission;
            // Validate bounds before sending
            if (SelectedEarliestUtc.Year < 2018 || SelectedEarliestUtc.Year > 2029)
            {
                ShowError($"Earliest landing year ({SelectedEarliestUtc.Year}) is outside valid backend range (2018–2029).");
                return;
            }

            DateTime maxBackendAllowedEnd = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime evaluatedEnd = SelectedEarliestUtc.AddHours(SelectedSearchWindowH + SelectedMissionDurationH);
            if (evaluatedEnd > maxBackendAllowedEnd)
            {
                ShowError($"Evaluation window ends at {evaluatedEnd:yyyy-MM-dd HH:mm} UTC, which exceeds backend limit of 2030-01-01T00:00:00Z.");
                return;
            }

            double totalSpan = SelectedSearchWindowH + SelectedMissionDurationH;
            if (totalSpan > 744.0)
            {
                ShowError($"Search span ({SelectedSearchWindowH}h) + Mission duration ({SelectedMissionDurationH}h) = {totalSpan:F1}h exceeds maximum allowed 744 hours.");
                return;
            }

            if (selectedSiteIds.Count == 0)
            {
                ShowError("Please select at least one candidate site.");
                return;
            }

            var req = new LiveOpportunityRequest
            {
                earliest_landing_utc = SelectedEarliestUtc.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture),
                search_window_h = SelectedSearchWindowH,
                mission_duration_h = SelectedMissionDurationH,
                candidate_site_ids = new List<string>(selectedSiteIds).ToArray(),
                min_sun_fraction = SliderMinSun.value / 100.0,
                min_earth_fraction = SliderMinEarth.value / 100.0,
                max_dark_h = SliderMaxDark.value,
                max_comm_gap_h = SliderMaxComm.value,
                search_step_h = 6.0,
                ranking_policy = SelectedPolicy,
                sampling_step_minutes = 10,
                observer_height_m = 2.0
            };

            ClearError();
            OnSearchRequested?.Invoke(req);
        }

        public void SetLoading(bool loading, string message = null)
        {
            IsLoading = loading;
            BtnSearch.text = loading ? "SEARCHING..." : "FIND OPPORTUNITIES";
            BtnSearch.SetEnabled(!loading);
            LoadingIndicator.style.display = loading ? DisplayStyle.Flex : DisplayStyle.None;
            if (message != null) LblLoadingStatus.text = message;
        }

        public void ShowError(string message)
        {
            SetLoading(false);
            ErrorBanner.style.display = DisplayStyle.Flex;
            LblErrorMessage.text = "⚠ " + message;
        }

        public void ClearError()
        {
            ErrorBanner.style.display = DisplayStyle.None;
            LblErrorMessage.text = "";
        }

        public void DisplayResults(LiveOpportunitiesResponse resp)
        {
            SetLoading(false);
            ClearError();
            ResultsScrollView.Clear();

            if (resp == null)
            {
                var lblNone = new Label("No opportunity search response received.");
                lblNone.AddToClassList("subtle");
                ResultsScrollView.Add(lblNone);
                return;
            }

            // Summary row
            var summaryRow = new VisualElement();
            summaryRow.AddToClassList("row");
            summaryRow.style.justifyContent = Justify.SpaceBetween;
            summaryRow.style.marginBottom = 6;

            var lblCounts = new Label($"Evaluated: {resp.evaluated_count}  ·  Qualifying: {resp.qualifying_count}");
            lblCounts.AddToClassList("subtle");
            summaryRow.Add(lblCounts);

            var lblBadge = new Label($"[{resp.data_status}]");
            lblBadge.style.color = new Color(0.44f, 0.89f, 0.69f);
            lblBadge.style.fontSize = 9;
            lblBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
            summaryRow.Add(lblBadge);
            ResultsScrollView.Add(summaryRow);

            // If we have qualifying recommendations
            if (resp.recommendations != null && resp.recommendations.Length > 0)
            {
                foreach (var rec in resp.recommendations)
                {
                    var card = new VisualElement();
                    card.AddToClassList("opp-card");

                    var cardHead = new VisualElement();
                    cardHead.AddToClassList("opp-head");

                    var sLabel = new Label($"#{rec.rank}  {rec.site_name ?? rec.site_id}");
                    sLabel.AddToClassList("opp-site");
                    cardHead.Add(sLabel);

                    var scLabel = new Label($"Score: {rec.score * 100.0:F1}");
                    scLabel.AddToClassList("opp-score");
                    cardHead.Add(scLabel);
                    card.Add(cardHead);

                    var tLabel = new Label($"Arrival: {rec.landing_utc} ({rec.mission_duration_h:F0}h mission)");
                    tLabel.AddToClassList("opp-time");
                    card.Add(tLabel);

                    if (rec.metrics != null)
                    {
                        var mRow = new VisualElement();
                        mRow.AddToClassList("opp-metrics-row");
                        mRow.Add(new Label($"Sun: {rec.metrics.f_sun * 100.0:F0}%") { name = "m-sun" });
                        mRow.Add(new Label($"Earth: {rec.metrics.f_earth * 100.0:F0}%") { name = "m-earth" });
                        mRow.Add(new Label($"Dark: {rec.metrics.dark_max_h:F1}h") { name = "m-dark" });
                        mRow.Add(new Label($"Comm Gap: {rec.metrics.comm_gap_max_h:F1}h") { name = "m-comm" });
                        card.Add(mRow);
                    }

                    if (!string.IsNullOrEmpty(rec.explanation))
                    {
                        var expLabel = new Label(rec.explanation);
                        expLabel.AddToClassList("subtle");
                        expLabel.style.fontSize = 9;
                        expLabel.style.marginTop = 3;
                        card.Add(expLabel);
                    }

                    card.RegisterCallback<ClickEvent>(e => OnRecommendationSelected?.Invoke(rec));
                    ResultsScrollView.Add(card);
                }
            }
            else
            {
                // Zero qualifying results
                var emptyBox = new VisualElement();
                emptyBox.style.paddingTop = 8;
                emptyBox.style.paddingBottom = 8;
                emptyBox.style.paddingLeft = 8;
                emptyBox.style.paddingRight = 8;
                emptyBox.style.backgroundColor = new Color(0.1f, 0.12f, 0.16f, 0.8f);
                emptyBox.style.borderTopLeftRadius = 6;
                emptyBox.style.borderTopRightRadius = 6;
                emptyBox.style.borderBottomLeftRadius = 6;
                emptyBox.style.borderBottomRightRadius = 6;
                emptyBox.style.marginBottom = 8;

                var lblEmptyTitle = new Label("0 QUALIFYING SCENARIOS");
                lblEmptyTitle.style.color = new Color(1f, 0.7f, 0.2f);
                lblEmptyTitle.style.fontSize = 11;
                lblEmptyTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                emptyBox.Add(lblEmptyTitle);

                string reason = !string.IsNullOrEmpty(resp.empty_reason)
                    ? resp.empty_reason
                    : "No candidate scenario met all four hard constraints.";
                var lblReason = new Label(reason);
                lblReason.AddToClassList("subtle");
                lblReason.style.whiteSpace = WhiteSpace.Normal;
                lblReason.style.marginTop = 3;
                emptyBox.Add(lblReason);
                ResultsScrollView.Add(emptyBox);

                // Show Closest Misses if available
                if (resp.closest_misses != null && resp.closest_misses.Length > 0)
                {
                    var lblMissHeader = new Label("CLOSEST NEAR-PASSING OPPORTUNITIES:");
                    lblMissHeader.AddToClassList("field-label");
                    lblMissHeader.style.marginTop = 6;
                    lblMissHeader.style.color = new Color(1f, 0.75f, 0.35f);
                    ResultsScrollView.Add(lblMissHeader);

                    var lblMissSub = new Label("Windows that came closest to qualifying, and what fell short:");
                    lblMissSub.AddToClassList("subtle");
                    lblMissSub.style.fontSize = 9;
                    lblMissSub.style.marginBottom = 4;
                    ResultsScrollView.Add(lblMissSub);

                    foreach (var miss in resp.closest_misses)
                    {
                        var missCard = new VisualElement();
                        missCard.AddToClassList("opp-card");
                        missCard.style.borderLeftColor = new Color(0.95f, 0.65f, 0.25f, 0.9f);
                        missCard.style.borderLeftWidth = 2;

                        var mHead = new VisualElement();
                        mHead.AddToClassList("opp-head");
                        mHead.Add(new Label(miss.site_name ?? miss.site_id) { style = { color = new Color(0.9f, 0.92f, 0.95f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });

                        var nearBadge = new Label("NEAR MATCH");
                        nearBadge.style.color = new Color(1f, 0.8f, 0.35f);
                        nearBadge.style.fontSize = 9;
                        nearBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
                        nearBadge.style.backgroundColor = new Color(0.3f, 0.22f, 0.08f, 0.6f);
                        nearBadge.style.borderTopLeftRadius = nearBadge.style.borderTopRightRadius = nearBadge.style.borderBottomLeftRadius = nearBadge.style.borderBottomRightRadius = 3;
                        nearBadge.style.paddingLeft = nearBadge.style.paddingRight = 4;
                        nearBadge.style.paddingTop = nearBadge.style.paddingBottom = 1;
                        mHead.Add(nearBadge);
                        missCard.Add(mHead);

                        string arrivalDisplay = miss.landing_utc;
                        if (DateTime.TryParse(miss.landing_utc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dtArrival))
                        {
                            arrivalDisplay = dtArrival.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";
                        }
                        var mTime = new Label($"Arrival: {arrivalDisplay}");
                        mTime.AddToClassList("opp-time");
                        missCard.Add(mTime);

                        if (miss.failed_constraints != null && miss.failed_constraints.Length > 0)
                        {
                            var failsList = new List<string>();
                            foreach (var fc in miss.failed_constraints)
                            {
                                if (fc == "min_sun_fraction")
                                    failsList.Add($"Sunlight below minimum {(miss.metrics != null ? $"({miss.metrics.f_sun * 100:0.#}%, req {SliderMinSun.value:0}%)" : $"(req {SliderMinSun.value:0}%)")}");
                                else if (fc == "min_earth_fraction")
                                    failsList.Add($"Earth line-of-sight below minimum {(miss.metrics != null ? $"({miss.metrics.f_earth * 100:0.#}%, req {SliderMinEarth.value:0}%)" : $"(req {SliderMinEarth.value:0}%)")}");
                                else if (fc == "max_dark_h")
                                    failsList.Add($"Continuous darkness exceeded limit {(miss.metrics != null ? $"({miss.metrics.dark_max_h:0.#} h, max {SliderMaxDark.value:0} h)" : $"(max {SliderMaxDark.value:0} h)")}");
                                else if (fc == "max_comm_gap_h")
                                    failsList.Add($"Comms outage exceeded limit {(miss.metrics != null ? $"({miss.metrics.comm_gap_max_h:0.#} h, max {SliderMaxComm.value:0} h)" : $"(max {SliderMaxComm.value:0} h)")}");
                                else
                                    failsList.Add(fc.Replace('_', ' '));
                            }
                            var fLabel = new Label("• " + string.Join("\n• ", failsList));
                            fLabel.style.color = new Color(1f, 0.65f, 0.55f);
                            fLabel.style.fontSize = 10;
                            fLabel.style.marginTop = 3;
                            fLabel.style.whiteSpace = WhiteSpace.Normal;
                            missCard.Add(fLabel);
                        }

                        var recFromMiss = new LiveOpportunityRecommendation
                        {
                            rank = 0,
                            site_id = miss.site_id,
                            site_name = miss.site_name,
                            landing_utc = miss.landing_utc,
                            mission_end_utc = miss.mission_end_utc,
                            mission_duration_h = miss.mission_duration_h,
                            metrics = miss.metrics,
                            score = 0,
                            explanation = "Closest near-passing scenario."
                        };
                        missCard.RegisterCallback<ClickEvent>(_ => OnRecommendationSelected?.Invoke(recFromMiss));
                        ResultsScrollView.Add(missCard);
                    }
                }
            }

            // Provenance and limitation footer
            var limBox = new Label("Provenance: nominal sphere GEOMETRIC_ONLY; no terrain obstruction or certified landing hazard analysis.");
            limBox.AddToClassList("metric-caption");
            limBox.style.marginTop = 8;
            ResultsScrollView.Add(limBox);
        }
    }
}
