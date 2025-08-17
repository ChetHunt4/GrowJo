using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace GrowJo.Helpers
{
    public static class GraphicsHelper
    {
        public static SKBitmap LoadBitmapFromFile(string filePath)
        {
            using (SKStream stream = new SKFileStream(filePath))
            {
                SKBitmap skBitmap = SKBitmap.Decode(stream);
                return skBitmap;
            }
        }

        public static BitmapSource GetBitmapFromSKBitmap(SKBitmap bitmap)
        {
            if (bitmap != null)
            {
                SKImageInfo imageInfo = new SKImageInfo(bitmap.Width, bitmap.Height);
                using (SKSurface surface = SKSurface.Create(imageInfo))
                {
                    SKCanvas canvas = surface.Canvas;
                    using (SKPaint paint = new SKPaint())
                    {
                        canvas.DrawBitmap(bitmap, 0, 0);
                    }
                    using (SKImage datImage = surface.Snapshot())
                    using (SKData data = datImage.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        byte[] imageBytes = data.ToArray();
                        BitmapSource bmsource = BitmapImageFromByteArray(imageBytes);

                        return bmsource;
                    }
                }
            }
            return null!;
        }

        public static SKBitmap Rotate(SKBitmap bitmap, double angle)
        {
            double radians = Math.PI * angle / 180;
            float sine = (float)Math.Abs(Math.Sin(radians));
            float cosine = (float)Math.Abs(Math.Cos(radians));
            int originalWidth = bitmap.Width;
            int originalHeight = bitmap.Height;
            int rotatedWidth = (int)(cosine * originalWidth + sine * originalHeight);
            int rotatedHeight = (int)(cosine * originalHeight + sine * originalWidth);

            var rotatedBitmap = new SKBitmap(rotatedWidth, rotatedHeight);

            using (var surface = new SKCanvas(rotatedBitmap))
            {
                surface.Clear();
                surface.Translate(rotatedWidth / 2, rotatedHeight / 2);
                surface.RotateDegrees((float)angle);
                surface.Translate(-originalWidth / 2, -originalHeight / 2);
                surface.DrawBitmap(bitmap, new SKPoint());
            }
            return rotatedBitmap;
        }

        public static BitmapSource BitmapImageFromByteArray(byte[] imageBytes)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new System.IO.MemoryStream(imageBytes);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            return bitmapImage;
        }

        public static bool SaveImageToFile(string filename, SKBitmap bitmap, SKEncodedImageFormat format)
        {
            try
            {
                using (var image = SKImage.FromBitmap(bitmap))
                {
                    using (var data = image.Encode(format, 100))
                    {
                        using (var stream = new System.IO.FileStream(filename, FileMode.Create))
                        {
                            data.SaveTo(stream);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.Write(ex.Message);
                return false;
            }
        }
    }

    public class CroppingRectangle
    {
        SKRect maxRect;             // generally the size of the bitmap
        float? aspectRatio;
        const float MINIMUM = 10;   // pixels width or height
        public const float CORNER = 50;

        public SKRect Rect { set; get; }

        public CroppingRectangle(SKRect maxRect, float? aspectRatio = null)
        {
            this.maxRect = maxRect;
            this.aspectRatio = aspectRatio;

            // Set initial cropping rectangle
            Rect = new SKRect(0.9f * maxRect.Left + 0.1f * maxRect.Right,
                              0.9f * maxRect.Top + 0.1f * maxRect.Bottom,
                              0.1f * maxRect.Left + 0.9f * maxRect.Right,
                              0.1f * maxRect.Top + 0.9f * maxRect.Bottom);

            // Adjust for aspect ratio
            if (aspectRatio.HasValue)
            {
                SKRect rect = Rect;
                float aspect = aspectRatio.Value;

                if (rect.Width > aspect * rect.Height)
                {
                    float width = aspect * rect.Height;
                    rect.Left = (maxRect.Width - width) / 2;
                    rect.Right = rect.Left + width;
                }
                else
                {
                    float height = rect.Width / aspect;
                    rect.Top = (maxRect.Height - height) / 2;
                    rect.Bottom = rect.Top + height;
                }

                Rect = rect;
            }
        }

        public SKPoint[] Corners
        {
            get
            {
                return new SKPoint[]
                {
                new SKPoint(Rect.Left, Rect.Top),
                new SKPoint(Rect.Right, Rect.Top),
                new SKPoint(Rect.Right, Rect.Bottom),
                new SKPoint(Rect.Left, Rect.Bottom)
                };
            }
        }

        public int HitTest(SKPoint point, float radius)
        {
            SKPoint[] corners = Corners;

            for (int index = 0; index < corners.Length; index++)
            {
                SKPoint diff = point - corners[index];

                if ((float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y) < radius)
                {
                    return index;
                }
            }

            return -1;
        }

        public void MoveCorner(int index, SKPoint point)
        {
            SKRect rect = Rect;

            switch (index)
            {
                case 0: // upper-left
                    rect.Left = Math.Min(Math.Max(point.X, maxRect.Left), rect.Right - MINIMUM);
                    rect.Top = Math.Min(Math.Max(point.Y, maxRect.Top), rect.Bottom - MINIMUM);
                    break;

                case 1: // upper-right
                    rect.Right = Math.Max(Math.Min(point.X, maxRect.Right), rect.Left + MINIMUM);
                    rect.Top = Math.Min(Math.Max(point.Y, maxRect.Top), rect.Bottom - MINIMUM);
                    break;

                case 2: // lower-right
                    rect.Right = Math.Max(Math.Min(point.X, maxRect.Right), rect.Left + MINIMUM);
                    rect.Bottom = Math.Max(Math.Min(point.Y, maxRect.Bottom), rect.Top + MINIMUM);
                    break;

                case 3: // lower-left
                    rect.Left = Math.Min(Math.Max(point.X, maxRect.Left), rect.Right - MINIMUM);
                    rect.Bottom = Math.Max(Math.Min(point.Y, maxRect.Bottom), rect.Top + MINIMUM);
                    break;
            }

            // Adjust for aspect ratio
            if (aspectRatio.HasValue)
            {
                float aspect = aspectRatio.Value;

                if (rect.Width > aspect * rect.Height)
                {
                    float width = aspect * rect.Height;

                    switch (index)
                    {
                        case 0:
                        case 3: rect.Left = rect.Right - width; break;
                        case 1:
                        case 2: rect.Right = rect.Left + width; break;
                    }
                }
                else
                {
                    float height = rect.Width / aspect;

                    switch (index)
                    {
                        case 0:
                        case 1: rect.Top = rect.Bottom - height; break;
                        case 2:
                        case 3: rect.Bottom = rect.Top + height; break;
                    }
                }
            }

            Rect = rect;
        }
    }

    
}
