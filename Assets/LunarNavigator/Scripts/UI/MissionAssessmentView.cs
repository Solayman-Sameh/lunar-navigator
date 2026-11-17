using System;
using System.Globalization;
using LunarNavigator.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace LunarNavigator.UI
{
    // Presentation only: consumes the scientific engine's metrics without changing them.
    public sealed class MissionAssessmentView : VisualElement
    {
        public Label TargetSite { get; } = new Label { name="assessment-target-site" };
        public Label Outcome { get; } = new Label { name="assessment-outcome" };
        public Label SunValue { get; } = new Label { name="assessment-sun-value" };
        public Label EarthValue { get; } = new Label { name="assessment-earth-value" };
        public Label SunStatus { get; } = new Label { name="assessment-sun-status" };
        public Label EarthStatus { get; } = new Label { name="assessment-earth-status" };
        public Label DarkValue { get; } = new Label { name="assessment-dark-value" };
        public Label GapValue { get; } = new Label { name="assessment-gap-value" };
        public Label DarkLimit { get; } = new Label { name="assessment-dark-limit" };
        public Label GapLimit { get; } = new Label { name="assessment-gap-limit" };
        public Label JointLabel { get; } = new Label { name="assessment-joint" };
        public Label Limitation { get; } = new Label { name="assessment-limitation" };
        public Label Conclusion { get; } = new Label { name="results-interpretation" };
        public Foldout Details { get; } = new Foldout { text="Detailed metrics",value=false,name="assessment-details" };
        readonly Label sunDescription=new Label {name="assessment-sun-description"};
        readonly Label earthDescription=new Label {name="assessment-earth-description"};
        readonly Label minimums=new Label {name="assessment-minimums"};

        public MissionAssessmentView(SkylineChartElement skyline)
        {
            name="mission-assessment";AddToClassList("mission-assessment");

            var targetBanner = new VisualElement { name = "assessment-target-banner" };
            targetBanner.AddToClassList("assessment-target-banner");
            var targetKicker = new Label("EVALUATED SITE");
            targetKicker.AddToClassList("assessment-target-label");
            targetBanner.Add(targetKicker);
            TargetSite.AddToClassList("assessment-target-val");
            TargetSite.text = "None selected";
            targetBanner.Add(TargetSite);
            Add(targetBanner);

            Outcome.AddToClassList("assessment-outcome");Add(Outcome);
            Add(Category("SUNLIGHT",SunStatus,SunValue,sunDescription,"assessment-sun"));
            Add(Category("EARTH LINE OF SIGHT",EarthStatus,EarthValue,earthDescription,"assessment-earth"));
            var conclusion=new VisualElement();conclusion.AddToClassList("assessment-conclusion");
            var caption=new Label("CONCLUSION");caption.AddToClassList("assessment-kicker");conclusion.Add(caption);
            conclusion.Add(Conclusion);Add(conclusion);
            Details.AddToClassList("assessment-foldout");
            Details.Add(Detail("LONGEST DARKNESS",DarkValue,DarkLimit,"assessment-dark-info",
                "The longest uninterrupted period when the Sun is below the modeled horizon."));
            Details.Add(Detail("COMMUNICATIONS GAP",GapValue,GapLimit,"assessment-gap-info",
                "The longest uninterrupted period when Earth is not directly visible from the selected location."));
            minimums.AddToClassList("assessment-secondary");Details.Add(minimums);
            JointLabel.AddToClassList("assessment-secondary");Details.Add(JointLabel);Add(Details);
            var sky=new Foldout {text="Skyline details",value=false,name="assessment-skyline"};
            sky.AddToClassList("assessment-foldout");
            sky.Add(new Label("Gold: Sun. Blue: Earth. Azimuth 0–360°; elevation −2 to 10°. This visualization does not validate local terrain obstruction."));
            sky.Add(skyline);Add(sky);
            var footer=new VisualElement();footer.AddToClassList("assessment-footer");
            Limitation.text="GEOMETRIC ONLY — terrain obstruction excluded.";
            footer.Add(Limitation);
            Add(footer);
            AddInfo(footer,this,"assessment-science-info",
                "Geometric Sun/Earth visibility only. Local DEM terrain obstruction is not validated. These results are not landing-safety certification. Earth line of sight alone does not guarantee a working communications link.");
            ShowUnavailable("Awaiting analysis","Select a location and analyze your mission window.");
        }

        static VisualElement Category(string title,Label status,Label value,Label description,string id)
        {
            var card=new VisualElement {name=id};card.AddToClassList("assessment-category");
            var header=new VisualElement();header.AddToClassList("assessment-category-header");
            var label=new Label(title);label.AddToClassList("assessment-kicker");header.Add(label);
            status.AddToClassList("assessment-badge");header.Add(status);card.Add(header);
            value.AddToClassList("assessment-value");card.Add(value);
            description.AddToClassList("assessment-description");card.Add(description);return card;
        }
        static VisualElement Detail(string title,Label value,Label limit,string id,string explanation)
        {
            var box=new VisualElement();box.AddToClassList("assessment-detail");
            var row=new VisualElement();row.AddToClassList("assessment-detail-heading");
            var label=new Label(title);label.AddToClassList("assessment-kicker");row.Add(label);box.Add(row);
            AddInfo(row,box,id,explanation);
            value.AddToClassList("assessment-detail-value");box.Add(value);
            limit.AddToClassList("assessment-secondary");box.Add(limit);return box;
        }
        static void AddInfo(VisualElement row,VisualElement content,string id,string text)
        {
            var explanation=new Label(text){name=id+"-explanation"};
            explanation.AddToClassList("assessment-explanation");explanation.style.display=DisplayStyle.None;
            var button=new Button {text="i",name=id,tooltip=text,focusable=true};
            button.AddToClassList("assessment-info");row.Add(button);content.Add(explanation);
            bool pinned=false;
            button.RegisterCallback<PointerEnterEvent>(_=>explanation.style.display=DisplayStyle.Flex);
            button.RegisterCallback<PointerLeaveEvent>(_=>{if(!pinned)explanation.style.display=DisplayStyle.None;});
            button.RegisterCallback<FocusInEvent>(_=>explanation.style.display=DisplayStyle.Flex);
            button.RegisterCallback<FocusOutEvent>(_=>{if(!pinned)explanation.style.display=DisplayStyle.None;});
            button.clicked+=()=>{pinned=!pinned;explanation.style.display=pinned?DisplayStyle.Flex:DisplayStyle.None;};
            button.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.Escape){pinned=false;explanation.style.display=DisplayStyle.None;e.StopPropagation();}});
        }
        static string Number(double value)=>value.ToString("0.#",CultureInfo.InvariantCulture);
        static void Badge(Label label,bool passed)
        {
            label.text=passed?"PASS":"FAIL";
            label.EnableInClassList("assessment-pass",passed);label.EnableInClassList("assessment-fail",!passed);
        }
        public void SetTarget(string siteName, string coords = "")
        {
            if (string.IsNullOrEmpty(coords))
                TargetSite.text = siteName;
            else
                TargetSite.text = $"{siteName} ({coords})";
        }
        public void SetLimits(double minSun,double minEarth,double maxDark,double maxGap)
        {
            DarkLimit.text="Configured maximum: "+Number(maxDark)+" h";
            GapLimit.text="Configured maximum: "+Number(maxGap)+" h";
            minimums.text="Minimum Sun: "+Number(minSun)+"% · Minimum Earth: "+Number(minEarth)+"%";
        }
        public void ShowUnavailable(string state,string explanation)
        {
            Outcome.text=state;
            if(state!="Analysis failed")Outcome.tooltip="";
            Outcome.EnableInClassList("assessment-pass",false);
            Outcome.EnableInClassList("assessment-fail",state=="Analysis failed");
            SunValue.text=EarthValue.text="—";DarkValue.text=GapValue.text="—";
            foreach(var label in new[]{SunStatus,EarthStatus}){label.text="NOT EVALUATED";label.RemoveFromClassList("assessment-pass");label.RemoveFromClassList("assessment-fail");}
            sunDescription.text=earthDescription.text="No current evaluated result.";
            JointLabel.text="";Conclusion.text=explanation;
        }
        public void Present(Metrics m,double minSun,double minEarth,double maxDark,double maxGap,string dataStatus)
        {
            Outcome.tooltip="Only your configured geometric visibility requirements; not landing-safety or radio-link certification.";
            bool sun=m.f_sun*100>=minSun && m.dark_max_h<=maxDark;
            bool earth=m.f_earth*100>=minEarth && m.comm_gap_max_h<=maxGap;
            Outcome.text=sun&&earth?"Requirements met":"Requirements not met";
            Outcome.EnableInClassList("assessment-pass",sun&&earth);Outcome.EnableInClassList("assessment-fail",!sun||!earth);
            Badge(SunStatus,sun);Badge(EarthStatus,earth);
            SunValue.text=Number(m.f_sun*100)+"% of mission";EarthValue.text=Number(m.f_earth*100)+"% of mission";
            DarkValue.text=Number(m.dark_max_h)+" h";GapValue.text=Number(m.comm_gap_max_h)+" h";
            SetLimits(minSun,minEarth,maxDark,maxGap);
            sunDescription.text=m.f_sun>=1 && m.dark_max_h==0?"No periods without sunlight.":m.f_sun<=0?"The Sun is below the modeled horizon throughout.":"Longest period without sunlight: "+Number(m.dark_max_h)+" h.";
            earthDescription.text=m.f_earth<=0?"Earth is not directly visible during this mission.":m.f_earth>=1 && m.comm_gap_max_h==0?"Earth is directly visible throughout this mission.":"Longest Earth-visibility gap: "+Number(m.comm_gap_max_h)+" h.";
            if(sun&&earth)Conclusion.text="This window meets your configured geometric sunlight and Earth-visibility requirements.";
            else if(sun)
            {
                string why=m.f_earth<=0?"Earth is not visible.":m.f_earth*100<minEarth?"Earth visibility is below your "+Number(minEarth)+"% minimum.":"the Earth-visibility gap is too long.";
                Conclusion.text="This window meets your sunlight requirements, but "+why;
                if(m.comm_gap_max_h>maxGap)Conclusion.text+=" The "+Number(m.comm_gap_max_h)+"-hour visibility gap exceeds your "+Number(maxGap)+"-hour limit.";
            }
            else if(earth)
            {
                Conclusion.text="This window meets your Earth-visibility requirements, but not your sunlight requirements.";
                Conclusion.text+=m.dark_max_h>maxDark?" The "+Number(m.dark_max_h)+"-hour darkness exceeds your "+Number(maxDark)+"-hour limit.":" Sunlight is below your "+Number(minSun)+"% minimum.";
            }
            else Conclusion.text="This window does not meet your sunlight or Earth-visibility requirements. Review the configured limits in Detailed metrics.";
            Limitation.text=dataStatus=="GEOMETRIC_ONLY"?"GEOMETRIC ONLY — terrain obstruction excluded.":"TERRAIN ESTIMATE — not landing-safety certification.";
        }
    }
}
