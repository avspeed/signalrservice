using System.ComponentModel;
using System.Configuration.Install;


namespace CentralServerNET
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}
