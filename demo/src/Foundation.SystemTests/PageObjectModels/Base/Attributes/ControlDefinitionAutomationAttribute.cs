using Atata;

namespace Foundation.SystemTests.PageObjectModels.Base.Attributes
{
    public class ControlDefinitionAutomationAttribute : ControlDefinitionAttribute
    {
        public ControlDefinitionAutomationAttribute(string automation) : base($"*[@automation='{automation}']")
        {
        }
    }
}
