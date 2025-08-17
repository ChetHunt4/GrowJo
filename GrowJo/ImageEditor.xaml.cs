using GrowJo.Helpers;
using SkiaSharp;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace GrowJo
{
    /// <summary>
    /// Interaction logic for ImageEditor.xaml
    /// </summary>
    public partial class ImageEditor : Window
    {
        private string Filename { get; set; }
        private SKBitmap? OriginalBitmapToEdit { get; set; }
        private SKBitmap? ResizedEditBitmap { get; set; }
        private CroppingRectangle? CropRectangle { get; set; }
        private bool CropMode { get; set; }
        private bool MouseLeftDown { get; set; }
        private int CropX { get; set; }
        private int CropY { get; set; }
        private int CropWidth { get; set; }
        private int CropHeight { get; set; }
        private bool StartedCrop { get; set; }

        private List<IImageCmd> SaveCommands { get; set; } = new List<IImageCmd>();

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
            Filename = filename;
            if (File.Exists(filename))
            {
                OriginalBitmapToEdit = GraphicsHelper.LoadBitmapFromFile(filename);
                if (OriginalBitmapToEdit != null)
                {
                    if (OriginalBitmapToEdit.Height > imgToEdit.MaxHeight)
                    {
                        float aspectWidth = (float)imgToEdit.MaxHeight / OriginalBitmapToEdit.Height;
                        ResizedEditBitmap = OriginalBitmapToEdit.Resize(new SKImageInfo((int)(OriginalBitmapToEdit.Width * aspectWidth), (int)imgToEdit.MaxHeight), SKFilterQuality.None);
                    }
                    else
                    {
                        //don't need to resize
                        ResizedEditBitmap = OriginalBitmapToEdit;
                    }
                }
                        imgToEdit.Source = GraphicsHelper.GetBitmapFromSKBitmap(ResizedEditBitmap!);
            }
        }


        private void btnRotateLeft_Click(object sender, RoutedEventArgs e)
        {
            ResizedEditBitmap = GraphicsHelper.Rotate(ResizedEditBitmap!, -90);
            imgToEdit.Source = GraphicsHelper.GetBitmapFromSKBitmap(ResizedEditBitmap);
            SaveCommands.Add(new ImageCmdRotate(-90));
        }

        private void btnRotateRight_Click(object sender, RoutedEventArgs e)
        {
            ResizedEditBitmap = GraphicsHelper.Rotate(ResizedEditBitmap!, 90);
            imgToEdit.Source = GraphicsHelper.GetBitmapFromSKBitmap(ResizedEditBitmap);
            SaveCommands.Add(new ImageCmdRotate(90));
        }

        private void btnCrop_Click(object sender, RoutedEventArgs e)
        {
            CropMode = btnCrop.IsChecked.HasValue && btnCrop.IsChecked.Value;
            if (!CropMode && StartedCrop)
            {
                StartedCrop = false;
                SKRect rect = new SKRect(CropX, CropY, CropWidth, CropHeight);
                SKImageInfo imageInfo = new SKImageInfo((int)rect!.Width, (int)rect.Height);
                using (SKSurface surface = SKSurface.Create(imageInfo))
                {
                    SKCanvas canvas = surface.Canvas;
                    SKRect SourceRect = rect;
                    SKRect DestRect = new SKRect(0, 0, rect.Width, rect.Height);
                    canvas.DrawBitmap(ResizedEditBitmap, SourceRect, DestRect);
                    using (SKImage datImage = surface.Snapshot())
                    using (SKData data = datImage.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        ResizedEditBitmap = SKBitmap.Decode(data);
                        SaveCommands.Add(new ImageCmdCrop(rect));
                        imgToEdit.Source = GraphicsHelper.GetBitmapFromSKBitmap(ResizedEditBitmap);
                    }
                }
            }
        }

        private void btnResize_Click(object sender, RoutedEventArgs e)
        {
            pnlResizeOptions.Visibility = Visibility.Visible;
        }

        private void imgToEdit_MouseMove(object sender, MouseEventArgs e)
        {
            if (MouseLeftDown)
            {
                if (CropMode)
                {
                    StartedCrop = true;
                    Point p = Mouse.GetPosition(imgToEdit);
                    CropWidth = (int)(CropX + p.X);
                    CropHeight = (int)(CropY + p.Y);
                    if(CropWidth < 0)
                    {
                        var temp = CropX;
                        CropX -= CropWidth;
                        CropWidth = temp;
                    }
                    if (CropHeight < 0)
                    {
                        var temp = CropY;
                        CropY -= CropHeight;
                        CropHeight = temp;
                    }
                    const int CORNER = 50;
                    CropRectangle = new CroppingRectangle(new SKRect(CropX, CropY, CropWidth, CropHeight));
                    SKImageInfo imageInfo = new SKImageInfo(ResizedEditBitmap!.Width, ResizedEditBitmap.Height);
                    using (SKSurface surface = SKSurface.Create(imageInfo))
                    {
                        SKCanvas canvas = surface.Canvas;
                        using (SKPaint paint = new SKPaint())
                        {
                            canvas.DrawBitmap(ResizedEditBitmap, 0, 0);

                            using (SKPath path = new SKPath())
                            {
                                path.MoveTo(CropRectangle.Rect.Left, CropRectangle.Rect.Top + CORNER);
                                path.LineTo(CropRectangle.Rect.Left, CropRectangle.Rect.Top);
                                path.LineTo(CropRectangle.Rect.Left + CORNER, CropRectangle.Rect.Top);

                                path.MoveTo(CropRectangle.Rect.Right - CORNER, CropRectangle.Rect.Top);
                                path.LineTo(CropRectangle.Rect.Right, CropRectangle.Rect.Top);
                                path.LineTo(CropRectangle.Rect.Right, CropRectangle.Rect.Top + CORNER);

                                path.MoveTo(CropRectangle.Rect.Right, CropRectangle.Rect.Bottom - CORNER);
                                path.LineTo(CropRectangle.Rect.Right, CropRectangle.Rect.Bottom);
                                path.LineTo(CropRectangle.Rect.Right - CORNER, CropRectangle.Rect.Bottom);

                                path.MoveTo(CropRectangle.Rect.Left + CORNER, CropRectangle.Rect.Bottom);
                                path.LineTo(CropRectangle.Rect.Left, CropRectangle.Rect.Bottom);
                                path.LineTo(CropRectangle.Rect.Left, CropRectangle.Rect.Bottom - CORNER);

                                canvas.DrawPath(path, cornerStroke);
                            }
                        }
                        SKImage image = surface.Snapshot();
                        //ResizedEditBitmap = SKBitmap.FromImage(image);
                        imgToEdit.Source = GraphicsHelper.GetBitmapFromSKBitmap(SKBitmap.FromImage(image));
                    }
                }
            }
        }

        private void imgToEdit_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point p = Mouse.GetPosition(imgToEdit);
            MouseLeftDown = true;
            CropX = (int)p.X;
            CropY = (int)p.Y;
        }

        private void imgToEdit_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MouseLeftDown = false;
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
            GraphicsHelper.SaveImageToFile(Filename, ResizedEditBitmap!, format);

        }

        private void btnSaveImageCopy_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
