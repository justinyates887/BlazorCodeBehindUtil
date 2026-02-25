using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace BlazorCodeBehindUtil
{
    public class GeneralOptions : DialogPage
    {
        [Category("Defaults")]
        [DisplayName("Create .razor.cs by default")]
        public bool CreateCS { get; set; } = true;

        [Category("Defaults")]
        [DisplayName("Create .razor.css by default")]
        public bool CreateCSS { get; set; } = true;

        [Category("Defaults")]
        [DisplayName("Create .razor.js by default")]
        public bool CreateJS { get; set; } = false;

        [Category("Templates")]
        [DisplayName("Use File Scoped Namespaces")]
        [Description("If true, uses 'namespace Name;'. If false, uses curly braces.")]
        public bool UseFileScopedNamespace { get; set; } = true;
    }
}