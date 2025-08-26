using GrowJoMobileImageSender.Utilities;

namespace GrowJoMobileImageSender
{
    public partial class MainPage : ContentPage
    {

        private LanSender lanSender = new LanSender();
        private string? targetIp;
        private int targetPort;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void btnDiscover_Clicked(object sender, EventArgs e)
        {
            var result = await this.lanSender.DiscoverAsync();
            if (result.HasValue)
            {
                targetIp = result.Value.ip;
                targetPort = result.Value.port;
                lblStatus.Text = $"Found desktop at {targetIp}:{targetPort}";
                btnSend.IsEnabled = true;
            }
            else
            {
                lblStatus.Text = "No desktop found.";
            }
        }

        private async void btnSend_Clicked(object sender, EventArgs e)
        {
            if (targetIp == null) { lblStatus.Text = "No desktop selected."; return; }

            var file = await FilePicker.PickAsync(new PickOptions { FileTypes = FilePickerFileType.Images });
            if (file == null) return;

            //await sender.SendFileAsync(targetIp, targetPort, file.FullPath);
            await this.lanSender.SendFileAsync(targetIp, targetPort, file.FullPath);
            lblStatus.Text = $"Sent {file.FileName}";
        }
    }

}
