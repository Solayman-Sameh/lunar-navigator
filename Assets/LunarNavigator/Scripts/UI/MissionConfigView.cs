using System;
using System.Globalization;
using LunarNavigator.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace LunarNavigator.UI
{
    public class MissionConfigView : VisualElement
    {
        // UI Elements
        public TextField TxtLandingUtc { get; private set; }
        public TextField TxtDurationHours { get; private set; }
        public TextField TxtApiUrl { get; private set; }
        public Button BtnAnalyze { get; private set; }
        public Button BtnCancel { get; private set; }

        public VisualElement LoadingIndicator { get; private set; }
        public Label LblLoadingStatus { get; private set; }
        public VisualElement ErrorBanner { get; private set; }
        public Label LblErrorMessage { get; private set; }

        // Quick buttons
        public Button Chip24 { get; private set; }
        public Button Chip72 { get; private set; }
        public Button Chip168 { get; private set; }

        public Button StepMinus1d { get; private set; }
        public Button StepMinus6h { get; private set; }
        public Button StepPlus6h { get; private set; }
        public Button StepPlus1d { get; private set; }

        // State
        public DateTime SelectedLandingUtc { get; private set; } = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        public double SelectedDurationHours { get; private set; } = 72.0;
        public bool IsLoading { get; private set; } = false;

        // Events
        public event Action<DateTime, double> OnAnalyzeRequested;
        public event Action OnCancelRequested;
        public event Action<DateTime, double> OnConfigChanged;

        public MissionConfigView()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            Clear();
            AddToClassList("mission-config-container");

            // 1. Landing Date & Time Input (Editable, not restricted to 14-day stub period)
            var timeRow = new VisualElement();
            timeRow.AddToClassList("field-row");
            timeRow.style.marginTop = 8;

            var lblTime = new Label("Landing UTC (yyyy-MM-dd HH:mm)");
            lblTime.AddToClassList("field-label");
            timeRow.Add(lblTime);

            TxtLandingUtc = new TextField();
            TxtLandingUtc.AddToClassList("text-input");
            TxtLandingUtc.value = SelectedLandingUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            TxtLandingUtc.RegisterValueChangedCallback(e => OnLandingTimeInputChanged(e.newValue));
            timeRow.Add(TxtLandingUtc);

            // Quick Step buttons
            var stepRow = new VisualElement();
            stepRow.AddToClassList("row");
            stepRow.style.marginTop = 4;

            StepMinus1d = new Button(() => StepTime(-86400)) { text = "-1d" }; StepMinus1d.AddToClassList("chip-btn"); stepRow.Add(StepMinus1d);
            StepMinus6h = new Button(() => StepTime(-21600)) { text = "-6h" }; StepMinus6h.AddToClassList("chip-btn"); stepRow.Add(StepMinus6h);
            StepPlus6h = new Button(() => StepTime(21600)) { text = "+6h" }; StepPlus6h.AddToClassList("chip-btn"); stepRow.Add(StepPlus6h);
            StepPlus1d = new Button(() => StepTime(86400)) { text = "+1d" }; StepPlus1d.AddToClassList("chip-btn"); stepRow.Add(StepPlus1d);
            timeRow.Add(stepRow);
            Add(timeRow);

            // 2. Mission Duration (Editable in hours, preserving exact intended interval)
            var durSection = new VisualElement();
            durSection.AddToClassList("field-row");
            durSection.style.marginTop = 6;

            var durLabel = new Label("Mission Duration (hours, 1 to 744)");
            durLabel.AddToClassList("field-label");
            durSection.Add(durLabel);

            var durInputRow = new VisualElement();
            durInputRow.style.flexDirection = FlexDirection.Row;
            durInputRow.style.alignItems = Align.Center;

            TxtDurationHours = new TextField();
            TxtDurationHours.AddToClassList("text-input");
            TxtDurationHours.style.width = 90;
            TxtDurationHours.value = SelectedDurationHours.ToString("F1", CultureInfo.InvariantCulture);
            TxtDurationHours.RegisterValueChangedCallback(e => OnDurationInputChanged(e.newValue));
            durInputRow.Add(TxtDurationHours);

            var durUnit = new Label("hours");
            durUnit.style.marginLeft = 6;
            durUnit.style.color = new Color(0.6f, 0.7f, 0.8f);
            durInputRow.Add(durUnit);

            durSection.Add(durInputRow);

            // Quick Duration Chips
            var chipRow = new VisualElement();
            chipRow.AddToClassList("chip-row");
            chipRow.style.marginTop = 4;

            Chip24 = new Button(() => SetDuration(24.0)) { text = "24 h" }; Chip24.AddToClassList("chip-btn"); chipRow.Add(Chip24);
            Chip72 = new Button(() => SetDuration(72.0)) { text = "72 h" }; Chip72.AddToClassList("chip-btn"); chipRow.Add(Chip72);
            Chip168 = new Button(() => SetDuration(168.0)) { text = "168 h" }; Chip168.AddToClassList("chip-btn"); chipRow.Add(Chip168);
            durSection.Add(chipRow);
            Add(durSection);

            // 3. API Configuration Foldout (Configurable endpoint)
            var apiFoldout = new Foldout { text = "Scientific API Connection", value = false };
            apiFoldout.style.marginTop = 4;

            var lblApi = new Label("FastAPI Endpoint URL");
            lblApi.AddToClassList("field-label");
            apiFoldout.Add(lblApi);

            TxtApiUrl = new TextField();
            TxtApiUrl.AddToClassList("text-input");
            TxtApiUrl.value = LunarApiClient.Instance.baseUrl;
            TxtApiUrl.RegisterValueChangedCallback(e =>
            {
                if (!string.IsNullOrEmpty(e.newValue))
                {
                    LunarApiClient.Instance.baseUrl = e.newValue.Trim();
                }
            });
            apiFoldout.Add(TxtApiUrl);

            var apiNote = new Label("Default: http://localhost:8000 (Local development service)");
            apiNote.AddToClassList("subtle");
            apiNote.style.marginTop = 3;
            apiFoldout.Add(apiNote);
            Add(apiFoldout);

            // 4. Loading State & Cancellation Element
            LoadingIndicator = new VisualElement();
            LoadingIndicator.AddToClassList("mission-loading-box");
            LoadingIndicator.style.display = DisplayStyle.None;

            LblLoadingStatus = new Label("Querying Horizons API...");
            LblLoadingStatus.style.color = new Color(0.7f, 0.88f, 1.0f);
            LblLoadingStatus.style.fontSize = 11;
            LoadingIndicator.Add(LblLoadingStatus);

            BtnCancel = new Button(HandleCancelClicked) { text = "Cancel ✕" };
            BtnCancel.AddToClassList("btn-cancel-small");
            LoadingIndicator.Add(BtnCancel);
            Add(LoadingIndicator);

            // 5. Error Banner
            ErrorBanner = new VisualElement();
            ErrorBanner.AddToClassList("mission-error-banner");
            ErrorBanner.style.display = DisplayStyle.None;

            var errHeader = new VisualElement();
            errHeader.style.flexDirection = FlexDirection.Row;
            errHeader.style.justifyContent = Justify.SpaceBetween;

            var errTitle = new Label("⚠ SCIENTIFIC API ERROR");
            errTitle.style.color = new Color(1f, 0.6f, 0.6f);
            errTitle.style.fontSize = 11;
            errTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            errHeader.Add(errTitle);

            var errDismiss = new Button(ClearError) { text = "×" };
            errDismiss.AddToClassList("icon-btn");
            errHeader.Add(errDismiss);
            ErrorBanner.Add(errHeader);

            LblErrorMessage = new Label();
            LblErrorMessage.style.color = new Color(0.95f, 0.85f, 0.85f);
            LblErrorMessage.style.fontSize = 11;
            LblErrorMessage.style.whiteSpace = WhiteSpace.Normal;
            LblErrorMessage.style.marginTop = 4;
            ErrorBanner.Add(LblErrorMessage);

            var errNotice = new Label("No synthetic fallback applied (scientific integrity enforced).");
            errNotice.style.color = new Color(0.8f, 0.5f, 0.5f);
            errNotice.style.fontSize = 9;
            errNotice.style.marginTop = 4;
            ErrorBanner.Add(errNotice);
            Add(ErrorBanner);

            // 6. Action Button
            BtnAnalyze = new Button(HandleAnalyzeClicked) { text = "ANALYZE MISSION WINDOW" };
            BtnAnalyze.AddToClassList("btn-primary");
            BtnAnalyze.style.marginTop = 10;
            Add(BtnAnalyze);

            UpdateChipStyles();
        }

        private void OnLandingTimeInputChanged(string text)
        {
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
            {
                TxtLandingUtc.style.borderBottomColor = new StyleColor(StyleKeyword.Null);
                SelectedLandingUtc = dt;
                ClearError();
                OnConfigChanged?.Invoke(SelectedLandingUtc, SelectedDurationHours);
            }
            else
            {
                TxtLandingUtc.style.borderBottomColor = new Color(0.9f, 0.2f, 0.2f, 1f);
            }
        }

        private void OnDurationInputChanged(string text)
        {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double h) && h > 0.0 && h <= 744.0)
            {
                TxtDurationHours.style.borderBottomColor = new StyleColor(StyleKeyword.Null);
                SelectedDurationHours = h;
                UpdateChipStyles();
                ClearError();
                OnConfigChanged?.Invoke(SelectedLandingUtc, SelectedDurationHours);
            }
            else
            {
                TxtDurationHours.style.borderBottomColor = new Color(0.9f, 0.2f, 0.2f, 1f);
            }
        }

        public void StepTime(double deltaSeconds)
        {
            SelectedLandingUtc = SelectedLandingUtc.AddSeconds(deltaSeconds);
            TxtLandingUtc.value = SelectedLandingUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            ClearError();
            OnConfigChanged?.Invoke(SelectedLandingUtc, SelectedDurationHours);
        }

        public void SetDuration(double hours)
        {
            SelectedDurationHours = hours;
            TxtDurationHours.value = hours.ToString("F1", CultureInfo.InvariantCulture);
            UpdateChipStyles();
            ClearError();
            OnConfigChanged?.Invoke(SelectedLandingUtc, SelectedDurationHours);
        }

        public void SetLandingUtc(DateTime utc)
        {
            SelectedLandingUtc = utc;
            TxtLandingUtc.value = utc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            ClearError();
        }

        private void UpdateChipStyles()
        {
            if (Chip24 != null) Chip24.EnableInClassList("chip-btn-active", Math.Abs(SelectedDurationHours - 24.0) < 0.01);
            if (Chip72 != null) Chip72.EnableInClassList("chip-btn-active", Math.Abs(SelectedDurationHours - 72.0) < 0.01);
            if (Chip168 != null) Chip168.EnableInClassList("chip-btn-active", Math.Abs(SelectedDurationHours - 168.0) < 0.01);
        }

        private void HandleAnalyzeClicked()
        {
            if (IsLoading) return;
            ClearError();
            OnAnalyzeRequested?.Invoke(SelectedLandingUtc, SelectedDurationHours);
        }

        private void HandleCancelClicked()
        {
            SetLoading(false);
            OnCancelRequested?.Invoke();
        }

        public void SetLoading(bool loading, string statusMessage = "Querying JPL Horizons & Science Engine...")
        {
            IsLoading = loading;
            if (LoadingIndicator != null)
            {
                LoadingIndicator.style.display = loading ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (LblLoadingStatus != null)
            {
                LblLoadingStatus.text = statusMessage;
            }
            if (BtnAnalyze != null)
            {
                BtnAnalyze.text = loading ? "ANALYZING..." : "ANALYZE MISSION WINDOW";
                BtnAnalyze.SetEnabled(!loading);
            }
        }

        public void ShowError(string message)
        {
            SetLoading(false);
            if (ErrorBanner != null)
            {
                ErrorBanner.style.display = DisplayStyle.Flex;
            }
            if (LblErrorMessage != null)
            {
                LblErrorMessage.text = message;
            }
        }

        public void ClearError()
        {
            if (ErrorBanner != null)
            {
                ErrorBanner.style.display = DisplayStyle.None;
            }
            if (LblErrorMessage != null)
            {
                LblErrorMessage.text = "";
            }
        }
    }
}
