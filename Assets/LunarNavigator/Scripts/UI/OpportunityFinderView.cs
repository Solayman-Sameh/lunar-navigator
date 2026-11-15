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
        public ScrollView ResultsScrollView { get; private set; }

        // State
        public DateTime SelectedEarliestUtc { get; private set; } = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        public double SelectedSearchWindowH { get; private set; } = 48.0;
        public double SelectedMissionDurationH { get; private set; } = 24.0;
        public bool IsLoading { get; private set; } = false;

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
            Add(sub);

            // 2. Earliest Landing Date/Time
            var lblEarliest = new Label("EARLIEST CANDIDATE ARRIVAL (UTC)");
            lblEarliest.AddToClassList("field-label");
            Add(lblEarliest);

            TxtEarliestUtc = new TextField();
            TxtEarliestUtc.AddToClassList("text-input");
            TxtEarliestUtc.value = SelectedEarliestUtc.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
            TxtEarliestUtc.RegisterValueChangedCallback(evt => ParseEarliestUtc(evt.newValue));
            Add(TxtEarliestUtc);

            // Quick date steps
            var dateStepRow = new VisualElement();
            dateStepRow.AddToClassList("chip-row");
            var bMinus1d = new Button(() => AdjustEarliestDays(-1)) { text = "-1d" };
            bMinus1d.AddToClassList("chip-btn");
            dateStepRow.Add(bMinus1d);
            var bMinus6h = new Button(() => AdjustEarliestHours(-6)) { text = "-6h" };
            bMinus6h.AddToClassList("chip-btn");
            dateStepRow.Add(bMinus6h);
            var bPlus6h = new Button(() => AdjustEarliestHours(6)) { text = "+6h" };
            bPlus6h.AddToClassList("chip-btn");
            dateStepRow.Add(bPlus6h);
            var bPlus1d = new Button(() => AdjustEarliestDays(1)) { text = "+1d" };
            bPlus1d.AddToClassList("chip-btn");
            dateStepRow.Add(bPlus1d);
            Add(dateStepRow);

            // 3. Durations (Search Window & Mission Duration)
            var durRow = new VisualElement();
            durRow.AddToClassList("row");
            durRow.style.justifyContent = Justify.SpaceBetween;
            durRow.style.marginBottom = 6;

            var boxWindow = new VisualElement { style = { width = Length.Percent(48) } };
            var lblWin = new Label("SEARCH SPAN (H)");
            lblWin.AddToClassList("field-label");
            boxWindow.Add(lblWin);
            TxtSearchWindowH = new TextField { value = SelectedSearchWindowH.ToString("F1") };
            TxtSearchWindowH.AddToClassList("text-input");
            TxtSearchWindowH.RegisterValueChangedCallback(evt =>
            {
                if (double.TryParse(evt.newValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var val) && val > 0 && val <= 744)
                {
                    SelectedSearchWindowH = val;
                }
            });
            boxWindow.Add(TxtSearchWindowH);
            durRow.Add(boxWindow);

            var boxMission = new VisualElement { style = { width = Length.Percent(48) } };
            var lblMis = new Label("MISSION DUR (H)");
            lblMis.AddToClassList("field-label");
            boxMission.Add(lblMis);
            TxtMissionDurationH = new TextField { value = SelectedMissionDurationH.ToString("F1") };
            TxtMissionDurationH.AddToClassList("text-input");
            TxtMissionDurationH.RegisterValueChangedCallback(evt =>
            {
                if (double.TryParse(evt.newValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var val) && val > 0 && val <= 744)
                {
                    SelectedMissionDurationH = val;
                }
            });
            boxMission.Add(TxtMissionDurationH);
            durRow.Add(boxMission);
            Add(durRow);

            // 4. Candidate Sites Chips
            var lblSites = new Label("CANDIDATE SITES");
            lblSites.AddToClassList("field-label");
            Add(lblSites);

            var siteChipsRow = new VisualElement();
            siteChipsRow.AddToClassList("chip-row");

            ChipSiteConnecting = new Button(() => ToggleSite("connecting_ridge", ChipSiteConnecting)) { text = "Connecting" };
            ChipSiteConnecting.AddToClassList("chip-btn");
            ChipSiteConnecting.AddToClassList("chip-btn-active");
            siteChipsRow.Add(ChipSiteConnecting);

            ChipSiteMalapert = new Button(() => ToggleSite("malapert_mountain", ChipSiteMalapert)) { text = "Malapert" };
            ChipSiteMalapert.AddToClassList("chip-btn");
            ChipSiteMalapert.AddToClassList("chip-btn-active");
            siteChipsRow.Add(ChipSiteMalapert);

            ChipSiteShackleton = new Button(() => ToggleSite("shackleton_rim", ChipSiteShackleton)) { text = "Shackleton" };
            ChipSiteShackleton.AddToClassList("chip-btn");
            ChipSiteShackleton.AddToClassList("chip-btn-active");
            siteChipsRow.Add(ChipSiteShackleton);
            Add(siteChipsRow);

            // 5. Constraints
            var lblCons = new Label("HARD CONSTRAINTS (ALL MUST PASS)");
            lblCons.AddToClassList("field-label");
            lblCons.style.marginTop = 4;
            Add(lblCons);

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
            var lblPol = new Label("RANKING POLICY");
            lblPol.AddToClassList("field-label");
            lblPol.style.marginTop = 6;
            Add(lblPol);

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
            ResultsScrollView = new ScrollView { name = "results-scroll" };
            ResultsScrollView.style.maxHeight = 260;
            ResultsScrollView.style.marginTop = 6;
            Add(ResultsScrollView);
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

        private void AdjustEarliestDays(int days)
        {
            SelectedEarliestUtc = SelectedEarliestUtc.AddDays(days);
            TxtEarliestUtc.value = SelectedEarliestUtc.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
        }

        private void AdjustEarliestHours(int hours)
        {
            SelectedEarliestUtc = SelectedEarliestUtc.AddHours(hours);
            TxtEarliestUtc.value = SelectedEarliestUtc.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
        }

        private void ParseEarliestUtc(string text)
        {
            string clean = text.Replace("UTC", "").Trim();
            if (DateTime.TryParse(clean, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
            {
                SelectedEarliestUtc = dt;
            }
        }

        public void SetEarliestUtc(DateTime utc)
        {
            SelectedEarliestUtc = utc.ToUniversalTime();
            TxtEarliestUtc.value = SelectedEarliestUtc.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
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
            SelectedSearchWindowH = searchWindowH;
            TxtSearchWindowH.value = searchWindowH.ToString("F1");
            SelectedMissionDurationH = missionDurationH;
            TxtMissionDurationH.value = missionDurationH.ToString("F1");

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
                earliest_landing_utc = SelectedEarliestUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
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
                    var lblMissHeader = new Label("CLOSEST MISSES:");
                    lblMissHeader.AddToClassList("field-label");
                    lblMissHeader.style.marginTop = 4;
                    ResultsScrollView.Add(lblMissHeader);

                    foreach (var miss in resp.closest_misses)
                    {
                        var missCard = new VisualElement();
                        missCard.AddToClassList("opp-card");
                        missCard.style.borderLeftColor = new Color(0.95f, 0.4f, 0.3f, 0.8f);
                        missCard.style.borderLeftWidth = 2;

                        var mHead = new VisualElement();
                        mHead.AddToClassList("opp-head");
                        mHead.Add(new Label(miss.site_name ?? miss.site_id) { style = { color = new Color(0.9f, 0.92f, 0.95f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
                        mHead.Add(new Label($"+{miss.normalized_violation:F2} viol.") { style = { color = new Color(0.95f, 0.45f, 0.35f), fontSize = 10 } });
                        missCard.Add(mHead);

                        var mTime = new Label($"Arrival: {miss.landing_utc}");
                        mTime.AddToClassList("opp-time");
                        missCard.Add(mTime);

                        if (miss.failed_constraints != null && miss.failed_constraints.Length > 0)
                        {
                            var fLabel = new Label("Failed: " + string.Join(", ", miss.failed_constraints));
                            fLabel.style.color = new Color(1f, 0.5f, 0.4f);
                            fLabel.style.fontSize = 9;
                            missCard.Add(fLabel);
                        }

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
