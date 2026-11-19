using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
namespace LunarNavigator.UI
{
    // Delayed editing keeps partial input out of the scientific request model.
    public class EditableNumberSlot : TextField
    {
        string accepted;
        public new string text { get => value; set { accepted=value.TrimEnd('%', 'h', ' '); SetValueWithoutNotify(accepted); } }
        public EditableNumberSlot(string initial)
        {
            isDelayed=true;
            text=initial;
            AddToClassList("editable-number-slot");
            textSelection.selectAllOnFocus=true;
            textSelection.selectAllOnMouseUp=true;
            RegisterCallback<KeyDownEvent>(e => {
                if(e.keyCode==KeyCode.Escape) { SetValueWithoutNotify(accepted); Blur(); e.StopPropagation(); }
                else if(e.keyCode==KeyCode.Return || e.keyCode==KeyCode.KeypadEnter) e.StopPropagation();
            });
        }
        public void Bind(Func<double,bool> commit, Action<string> error, bool whole=true)
        {
            this.RegisterValueChangedCallback(e => {
                string previous=accepted;
                if(!double.TryParse(e.newValue,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var number)
                   || double.IsNaN(number) || double.IsInfinity(number) || (whole && number!=Math.Truncate(number)) || !commit(number))
                {
                    text=previous;
                    error?.Invoke("Enter a valid " + (whole ? "whole number" : "number") + " for " + name.Replace("val-", "") + ". Previous value restored.");
                }
            });
        }
        public static bool ReplaceDatePart(DateTime current,string part,int number,out DateTime result)
        {
            result=current;
            try {
                int y=part=="year"?number:current.Year;
                if(y<2018 || y>2029)return false;
                result=new DateTime(y,part=="month"?number:current.Month,part=="day"?number:current.Day,
                    part=="hour"?number:current.Hour,part=="minute"?number:current.Minute,
                    part=="second"?number:current.Second,DateTimeKind.Utc);
                return true;
            } catch(ArgumentOutOfRangeException) {return false;}
        }
    }
}
