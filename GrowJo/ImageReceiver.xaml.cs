using GrowJo.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Win32;
using QRCoder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GrowJo
{
    /// <summary>
    /// Interaction logic for ImageReceiver.xaml
    /// </summary>
    public partial class ImageReceiver : Window
    {
        private string? ContentFolder { get; set; }
        private LanReciever? Reciever { get; set; }

        public ImageReceiver()
        {
            InitializeComponent();
            Initialize();
        }

        private void btnOpenContent_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolder = new OpenFolderDialog();
            if (openFolder.ShowDialog() == true) {
                ContentFolder = openFolder.FolderName;
                lblContent.Content = ContentFolder;
                pnlContentSelect.Visibility = Visibility.Collapsed;
                pnlImageReceiver.Visibility = Visibility.Visible;
                Reciever = new LanReciever(ContentFolder);
                lbReceived.Items.Clear();
                Reciever.FileReceived += path =>
                {
                    Dispatcher.Invoke(() => lbReceived.Items.Add($"{path}"));
                };
                Reciever.Start();
            }
        }

        private void Initialize()
        {
            //_listener = new HttpListener();
            //_listener.Prefixes.Add("http://*:" + Port.ToString() + "/");

            
        }
    }
}
