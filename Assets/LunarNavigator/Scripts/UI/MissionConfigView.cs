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
        public DateTime SelectedLandingUtc { get; private set; } = DemoSimulation.DeviceUtcSecond;
        public double SelectedDurationHours { get; private set; } = 72.0;
        public bool IsLoading { get; private set; } = false;

        // Aerospace Stepper Labels
        private Label lblYearVal;
        private Label lblMonthVal;
        private Label lblDayVal;
        private Label lblHourVal;
        private Label lblMinuteVal;
        private Label lblSecondVal;
        public event Action OnCurrentTimeRequested;

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

            // 1. Aerospace Segmented Datetime Stepper
            var timeRow = new VisualElement();
            timeRow.AddToClassList("field-row");
            timeRow.style.marginTop = 6;

            var lblTime = new Label("2  Landing date and time (UTC)");
            lblTime.AddToClassList("field-label");
            timeRow.Add(lblTime);

            // Keep TxtLandingUtc synchronized as internal data binding
            TxtLandingUtc = new TextField();
            TxtLandingUtc.style.display = DisplayStyle.None;
            TxtLandingUtc.value = SelectedLandingUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            timeRow.Add(TxtLandingUtc);

            var stepperRow = new VisualElement();
            stepperRow.AddToClassList("datetime-stepper-row");

            lblYearVal = new Label(SelectedLandingUtc.Year.ToString("D4"));
            lblMonthVal = new Label(SelectedLandingUtc.Month.ToString("D2"));
            lblDayVal = new Label(SelectedLandingUtc.Day.ToString("D2"));
            lblHourVal = new Label(SelectedLandingUtc.Hour.ToString("D2"));
            lblMinuteVal = new Label(SelectedLandingUtc.Minute.ToString("D2"));
            lblSecondVal = new Label(SelectedLandingUtc.Second.ToString("D2"));

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
            var useNow = new Button(() => { SetLandingUtc(DemoSimulation.DeviceUtcSecond); OnCurrentTimeRequested?.Invoke(); }) {
                text = "Use current UTC", name = "mission-use-current-utc",
                tooltip = "Set the mission start from this device's current UTC clock and resume 1x playback."
            };
            useNow.AddToClassList("chip-btn");
            timeRow.Add(useNow);
            Add(timeRow);

            // 2. Mission Duration (Editable in hours, preserving exact intended interval)
            var durSection = new VisualElement();
            durSection.AddToClassList("field-row");
            durSection.style.marginTop = 6;

            var durLabel = new Label("3  Mission duration (hours)");
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

            var minus=new Button(()=>SetDuration(Math.Max(1,SelectedDurationHours-1))){text="-",tooltip="One hour shorter"};minus.AddToClassList("stepper");
            var plus=new Button(()=>SetDuration(Math.Min(744,SelectedDurationHours+1))){text="+",tooltip="One hour longer"};plus.AddToClassList("stepper");
            durInputRow.Insert(0,minus);durInputRow.Add(plus);durSection.Add(durInputRow);

            // Quick Duration Chips
            var chipRow = new VisualElement();
            chipRow.AddToClassList("chip-row");
            chipRow.style.marginTop = 4;

            Chip24 = new Button(() => SetDuration(24.0)) { text = "24 h" }; Chip24.AddToClassList("chip-btn"); chipRow.Add(Chip24);
            Chip72 = new Button(() => SetDuration(72.0)) { text = "72 h" }; Chip72.AddToClassList("chip-btn"); chipRow.Add(Chip72);
            Chip168 = new Button(() => SetDuration(168.0)) { text = "168 h" }; Chip168.AddToClassList("chip-btn"); chipRow.Add(Chip168);
            chipRow.style.display=DisplayStyle.None;
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
            errNotice.style.display=DisplayStyle.None;
            Add(ErrorBanner);

            // 6. Action Button
            BtnAnalyze = new Button(HandleAnalyzeClicked) { text = "ANALYZE MISSION WINDOW" };
            BtnAnalyze.AddToClassList("btn-primary");
            BtnAnalyze.style.marginTop = 10;
            Add(BtnAnalyze);

            Add(apiFoldout);
            ConfigureInput(TxtLandingUtc);ConfigureInput(TxtDurationHours);ConfigureInput(TxtApiUrl);
            LblLoadingStatus.style.whiteSpace=WhiteSpace.Normal;LblLoadingStatus.style.flexShrink=1;
            UpdateChipStyles();
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
            try { SelectedLandingUtc = SelectedLandingUtc.AddYears(delta); } catch {}
            UpdateStepperDisplay();
        }

        public void StepMonth(int delta)
        {
            try { SelectedLandingUtc = SelectedLandingUtc.AddMonths(delta); } catch {}
            UpdateStepperDisplay();
        }

        public void StepDay(int delta)
        {
            try { SelectedLandingUtc = SelectedLandingUtc.AddDays(delta); } catch {}
            UpdateStepperDisplay();
        }

        public void StepHour(int delta)
        {
            try { SelectedLandingUtc = SelectedLandingUtc.AddHours(delta); } catch {}
            UpdateStepperDisplay();
        }

        public void StepMinute(int delta)
        {
            try { SelectedLandingUtc = SelectedLandingUtc.AddMinutes(delta); } catch {}
            UpdateStepperDisplay();
        }

        private void UpdateStepperDisplay()
        {
            if (lblYearVal != null) lblYearVal.text = SelectedLandingUtc.Year.ToString("D4");
            if (lblMonthVal != null) lblMonthVal.text = SelectedLandingUtc.Month.ToString("D2");
            if (lblDayVal != null) lblDayVal.text = SelectedLandingUtc.Day.ToString("D2");
            if (lblHourVal != null) lblHourVal.text = SelectedLandingUtc.Hour.ToString("D2");
            if (lblMinuteVal != null) lblMinuteVal.text = SelectedLandingUtc.Minute.ToString("D2");
            if (lblSecondVal != null) lblSecondVal.text = SelectedLandingUtc.Second.ToString("D2");

            if (TxtLandingUtc != null)
            {
                TxtLandingUtc.value = SelectedLandingUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
            ClearError();
            OnConfigChanged?.Invoke(SelectedLandingUtc, SelectedDurationHours);
        }

        public static void ConfigureInput(TextField field)
        {
            // Caret and selection colors are set by Navigator.uss.
            field.textSelection.selectAllOnFocus=false;field.textSelection.selectAllOnMouseUp=false;
        }
        private void OnLandingTimeInputChanged(string text)
        {
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
            {
                TxtLandingUtc.style.borderBottomColor = new StyleColor(StyleKeyword.Null);
                SelectedLandingUtc = dt;
                UpdateStepperDisplay();
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
            UpdateStepperDisplay();
        }

        public void StepSecond(int delta)
        {
            try { SelectedLandingUtc = SelectedLandingUtc.AddSeconds(delta); } catch (ArgumentOutOfRangeException) { }
            UpdateStepperDisplay();
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
            UpdateStepperDisplay();
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
            if(!DateTime.TryParseExact(TxtLandingUtc.value,new[]{"yyyy-MM-dd HH:mm","yyyy-MM-dd HH:mm:ss"},CultureInfo.InvariantCulture,DateTimeStyles.AdjustToUniversal|DateTimeStyles.AssumeUniversal,out var utc) ||
               !double.TryParse(TxtDurationHours.value,NumberStyles.Float,CultureInfo.InvariantCulture,out var hours) || double.IsNaN(hours)||double.IsInfinity(hours)||hours<1||hours>744)
            {ShowError("Enter a UTC date/time (yyyy-MM-dd HH:mm:ss) and 1-744 mission hours.");return;}
            SelectedLandingUtc=utc;SelectedDurationHours=hours;
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
