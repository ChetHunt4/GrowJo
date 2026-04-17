using GrowJo.Helpers;
using GrowJo.Utilities;
using Microsoft.Graph.Models;
using Microsoft.Win32;
using SkiaSharp;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static SkiaSharp.SKImageFilter;

namespace GrowJo
{
    /// <summary>
    /// Interaction logic for ImageEditor.xaml
    /// </summary>
    public partial class ImageEditor : Window
    {
        private string Filename { get; set; }
        private SKBitmap? OriginalBitmapToEdit { get; set; }
        //private SKBitmap? ResizedEditBitmap { get; set; }
        //private CroppingRectangle? CropRectangle { get; set; }
        //private bool CropMode { get; set; }
        private bool MouseLeftDown { get; set; }
        //private int CropX { get; set; }
        //private int CropY { get; set; }
        //private int CropWidth { get; set; }
        //private int CropHeight { get; set; }
        //private bool StartedCrop { get; set; }
        //private int Angle { get; set; }

        private ImageEditState ImageEditState { get; set; }

        //private List<IImageCmd> SaveCommands { get; set; } = new List<IImageCmd>();

        SKPaint cornerStroke = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.White,
            StrokeWidth = 10
        };

        SKPaint edgeStroke = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.White,
            StrokeWidth = 2
        };


        public ImageEditor(string filename)
        {
            InitializeComponent();
            ImageEditState = new ImageEditState();
            Filename = filename;
            if (File.Exists(filename))
            {
                OriginalBitmapToEdit = GraphicsHelper.LoadBitmapFromFile(filename);
                ImageEditState.OriginalImage = OriginalBitmapToEdit;

                if (OriginalBitmapToEdit != null)
                {
                    ImageEditState.ProxyImage = ImageEditState.CreateProxy(ImageEditState.OriginalImage, (int)Math.Max(Math.Max(imgToEdit.MaxWidth, imgToEdit.MaxHeight), 1000));

                }
            }
            var source = ImageEditState.Render();
            imgToEdit.Source = source;
            imgToEdit.Width = source.PixelWidth;
            imgToEdit.Height = source.PixelHeight;

        }

        private void btnRotateLeft_Click(object sender, RoutedEventArgs e)
        {
            ImageEditState.Angle -= 90f;
            ImageEditState.Angle %= 360;
            var source = ImageEditState.Render();
            imgToEdit.Source = source;
            imgToEdit.Width = source.PixelWidth;
            imgToEdit.Height = source.PixelHeight;
        }

        private void btnRotateRight_Click(object sender, RoutedEventArgs e)
        {
            ImageEditState.Angle += 90f;
            ImageEditState.Angle %= 360;

            var source = ImageEditState.Render();
            imgToEdit.Source = source;
            imgToEdit.Width = source.PixelWidth;
            imgToEdit.Height = source.PixelHeight;
        }

        private void btnCrop_Click(object sender, RoutedEventArgs e)
        {
            ImageEditState.IsCropping = btnCrop.IsChecked.HasValue && btnCrop.IsChecked.Value;
            if (!ImageEditState.IsCropping)
            {
                ImageEditState.CommitCrop();
                var rendered = ImageEditState.Render();
                imgToEdit.Source = rendered;
                imgToEdit.Width = rendered.PixelWidth;
                imgToEdit.Height = rendered.PixelHeight;
            }
            else
            {
                //initialize crop rect
                ImageEditState.InitCropRect();
            }
            imgToEdit.Source = ImageEditState.Render();
        }

        private void btnResize_Click(object sender, RoutedEventArgs e)
        {
            pnlResizeOptions.Visibility = Visibility.Visible;
        }

        private void imgToEdit_MouseMove(object sender, MouseEventArgs e)
        {
            if (ImageEditState.CurrentDrag == DragMode.None)
            {
                return;
            }
            var mousePos = e.GetPosition(imgToEdit);
            var bounds = ImageEditState.CurrentTransform.MapRect(new SKRect(0, 0, ImageEditState!.ProxyImage!.Width, ImageEditState!.ProxyImage.Height)).Standardized;
            float scaleX = bounds.Width / (float)imgToEdit.ActualWidth;
            float scaleY = bounds.Height / (float)imgToEdit.ActualHeight;

            var bmpPt = new SKPoint((float)mousePos.X * scaleX, (float)mousePos.Y * scaleY);
            bmpPt.Offset(bounds.Left, bounds.Top);

            var imgPt = ImageEditState.InverseTransform.MapPoint(bmpPt);

            var delta = new SKPoint(
                imgPt.X - ImageEditState.LastMousePos.X,
                imgPt.Y - ImageEditState.LastMousePos.Y);

            switch (ImageEditState.CurrentDrag)
            {
                case DragMode.Move:
                    ImageEditState.CropRect!.Value.Offset(delta);
                    break;

                case DragMode.Left:
                    ImageEditState.CropRect = new SKRect
                    {
                        Bottom = ImageEditState.CropRect!.Value.Bottom,
                        Left = ImageEditState.CropRect.Value.Left + delta.X,
                        Right = ImageEditState.CropRect.Value.Right,
                        Top = ImageEditState.CropRect.Value.Top
                    };
                    break;

                case DragMode.Right:
                    ImageEditState.CropRect = new SKRect
                    {
                        Bottom = ImageEditState.CropRect!.Value.Bottom,
                        Left = ImageEditState.CropRect.Value.Left,
                        Right = ImageEditState.CropRect.Value.Right + delta.X,
                        Top = ImageEditState.CropRect.Value.Top
                    };
                    break;

                case DragMode.Top:
                    ImageEditState.CropRect = new SKRect
                    {
                        Bottom = ImageEditState.CropRect!.Value.Bottom,
                        Left = ImageEditState.CropRect.Value.Left,
                        Right = ImageEditState.CropRect.Value.Right,
                        Top = ImageEditState.CropRect.Value.Top + delta.Y
                    };
                    break;

                case DragMode.Bottom:
                    ImageEditState.CropRect = new SKRect
                    {
                        Bottom = ImageEditState.CropRect!.Value.Bottom + delta.Y,
                        Left = ImageEditState.CropRect.Value.Left,
                        Right = ImageEditState.CropRect.Value.Right,
                        Top = ImageEditState.CropRect.Value.Top
                    };
                    break;

                case DragMode.TopLeft:
                    ImageEditState.CropRect = new SKRect
                    {
                        Bottom = ImageEditState.CropRect!.Value.Bottom,
                        Left = ImageEditState.CropRect.Value.Left + delta.X,
                        Right = ImageEditState.CropRect.Value.Right,
                        Top = ImageEditState.CropRect.Value.Top + delta.Y
                    };
                    break;

                case DragMode.TopRight:
                    ImageEditState.CropRect = new SKRect
                    {
                        Bottom = ImageEditState.CropRect!.Value.Bottom,
                        Left = ImageEditState.CropRect.Value.Left,
                        Right = ImageEditState.CropRect.Value.Right + delta.X,
                        Top = ImageEditState.CropRect.Value.Top + delta.Y
                    };
                    break;

                case DragMode.BottomLeft:
                    ImageEditState.CropRect = new SKRect
                    {
                        Bottom = ImageEditState.CropRect!.Value.Bottom + delta.Y,
                        Left = ImageEditState.CropRect.Value.Left + delta.X,
                        Right = ImageEditState.CropRect.Value.Right,
                        Top = ImageEditState.CropRect.Value.Top
                    };
                    break;

                case DragMode.BottomRight:
                    ImageEditState.CropRect = new SKRect
                    {
                        Bottom = ImageEditState.CropRect!.Value.Bottom + delta.Y,
                        Left = ImageEditState.CropRect.Value.Left,
                        Right = ImageEditState.CropRect.Value.Right + delta.X,
                        Top = ImageEditState.CropRect.Value.Top
                    };
                    break;

            }

            ImageEditState.LastMousePos = imgPt;
            imgToEdit.Source = ImageEditState.Render();
        }

        private void imgToEdit_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ImageEditState.IsCropping)
            {
                var mousePos = e.GetPosition(imgToEdit);
                var bounds = ImageEditState.CurrentTransform.MapRect(new SKRect(0, 0, ImageEditState.ProxyImage!.Width, ImageEditState.ProxyImage.Height)).Standardized;

                float scaleX = bounds.Width / (float)imgToEdit.ActualWidth;
                float scaleY = bounds.Height / (float)imgToEdit.ActualHeight;
                var bmpPt = new SKPoint((float)mousePos.X * scaleX, (float)mousePos.Y * scaleY);
                bmpPt.Offset(bounds.Left, bounds.Top);
                var imgPt = ImageEditState.InverseTransform.MapPoint(bmpPt);
                ImageEditState.CurrentDrag = ImageEditState.HitTest(imgPt);
                ImageEditState.LastMousePos = imgPt;
            }
        }

        private void imgToEdit_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ImageEditState.CurrentDrag = DragMode.None;
            ImageEditState.NormalizeCropRect();
        }

  

        private void txtImageWidth_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void txtImageHeight_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void btnSaveResize_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnCancelResize_Click(object sender, RoutedEventArgs e)
        {
            txtImageWidth.Text = String.Empty;
            txtImageHeight.Text = String.Empty;
            pnlResizeOptions.Visibility = Visibility.Collapsed;
        }

        private void btnSaveImage_Click(object sender, RoutedEventArgs e)
        {
            var extension = Path.GetExtension(Filename);
            SKEncodedImageFormat format = SKEncodedImageFormat.Png;
            switch (extension.ToUpper())
            {
                case ".JPG":
                case ".JPEG":
                    format = SKEncodedImageFormat.Jpeg;
                    break;
                case ".PNG":
                    format = SKEncodedImageFormat.Png;
                    break;
            }
            //for now save resized.
            GraphicsHelper.SaveImageToFile(Filename, ImageEditState.OriginalImage!, format);

        }

        private void btnSaveImageCopy_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog();
            saveDialog.Filter = "PNG Files(*.PNG)| *.PNG;| JPG Files(*.JPG)| *.JPG| All files(*.*) | *.*";
            saveDialog.DefaultExt = ".png";
            var saveResult = saveDialog.ShowDialog();
            if (saveResult == true)
            {
                var extension = Path.GetExtension(saveDialog.FileName);
                SKEncodedImageFormat format = SKEncodedImageFormat.Png;
                switch (extension.ToUpper())
                {
                    case ".JPG":
                    case ".JPEG":
                        format = SKEncodedImageFormat.Jpeg;
                        break;
                    case ".PNG":
                        format = SKEncodedImageFormat.Png;
                        break;
                }
                GraphicsHelper.SaveImageToFile(saveDialog.FileName, ImageEditState.OriginalImage!, format);
            }
        }

        private void imgToEdit_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (imgToEdit.ActualWidth > 0 && imgToEdit.ActualHeight > 0)
            {
                ImageEditState.Render();
            }
        }
    }
}
