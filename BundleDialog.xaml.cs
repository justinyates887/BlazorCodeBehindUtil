using System.Windows;

namespace BlazorCodeBehindUtil
{
    public partial class BundleDialog : Window
    {
        public string ComponentName { get; private set; }
        public bool CreateCS { get; private set; }
        public bool CreateCSS { get; private set; }
        public bool CreateJS { get; private set; }

        public BundleDialog(string defaultName, bool cs, bool css, bool js)
        {
            InitializeComponent();
            ComponentNameInput.Text = defaultName;
            ComponentNameInput.Focus();
            CheckCS.IsChecked = cs;
            CheckCSS.IsChecked = css;
            CheckJS.IsChecked = js;
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            ComponentName = ComponentNameInput.Text;
            CreateCS = CheckCS.IsChecked ?? false;
            CreateCSS = CheckCSS.IsChecked ?? false;
            CreateJS = CheckJS.IsChecked ?? false;
            DialogResult = true;
        }
    }
}