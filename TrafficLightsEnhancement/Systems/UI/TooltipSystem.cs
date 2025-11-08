using System.Collections.Generic;
using Game.UI.Tooltip;

namespace C2VM.TrafficLightsEnhancement.Systems.UI
{
    public partial class TooltipSystem : TooltipSystemBase
    {
        public List<StringTooltip> m_TooltipList;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TooltipList = [];
        }

        protected override void OnUpdate()
        {
            foreach (var tooltip in m_TooltipList)
            {
                AddMouseTooltip(tooltip);
            }
        }
    }
}