using GrowJo.Helpers;
using Microsoft.Graph.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Converters;
using System.Windows.Media.Imaging;

namespace GrowJo.Utilities
{
    //public interface IImageOperation
    //{
    //    SKBitmap Apply(SKBitmap input);
    //}
    public enum DragMode
    {
        None,
        Move,
        Left, Right, Top, Bottom,
        TopLeft, TopRight, BottomLeft, BottomRight
    }


    public class ImageEditState
    {
        public SKBitmap? OriginalImage { get; set; }
        public SKBitmap? ProxyImage { get; set; }
        //public List<IImageOperation> Operations { get; } = new List<IImageOperation>();
        //public SKMatrix Transform { get; set; }
        public SKRect? CropRect { get; set; }

        public SKMatrix CurrentTransform;
        public SKMatrix InverseTransform;

        public float Angle { get; set; } = 0f;

        public bool IsCropping { get; set; } = false;



        public DragMode CurrentDrag { get; set; }
        public SKPoint LastMousePos { get; set; }

        public SKBitmap ConfirmOperationsFullResolution()
        {
            SKBitmap current = OriginalImage!;
            float scaleX = (float)OriginalImage!.Width / ProxyImage!.Width;
            float scaleY = (float)OriginalImage.Height / ProxyImage.Height;
            if (CropRect == null)
            {
                CropRect = new SKRect(0, 0, ProxyImage.Width, ProxyImage.Height);
            }
            SKRect originalCrop = new SKRect(
                CropRect!.Value.Left * scaleX,
                CropRect!.Value.Top * scaleY,
                CropRect!.Value.Right * scaleX,
                CropRect!.Value.Bottom * scaleY
            );
            return current;
        }

        public static SKBitmap CreateProxy(SKBitmap original, int maxDimension = 2048)
        {
            float scale = Math.Min((float)maxDimension / original.Width, (float)maxDimension / original.Height);
            if (scale >= 1f)
            {
                return original.Copy();
            }

            int width = (int)(original.Width * scale);
            int height = (int)(original.Height * scale);

            return original.Resize(new SKImageInfo(width, height), SKFilterQuality.Medium);
        }

        public SKMatrix CreateTransform(float angle)
        {
            return SKMatrix.CreateRotationDegrees(angle);
        }

        public void UpdateTransform()
        {
            CurrentTransform = SKMatrix.CreateRotationDegrees(Angle);
            if (!CurrentTransform.TryInvert(out var inv))
            {
                inv = SKMatrix.CreateIdentity();
            }
            InverseTransform = inv;
        }

        //public SKMatrix GetTransform()
        //{
        //    return Transform;
        //}

        public void InitCropRect()
        {
            CropRect = new SKRect(0, 0, ProxyImage!.Width, ProxyImage.Height);
        }

        public void ClampCropRect()
        {
            const float minSize = 10f;

            CropRect = new SKRect
            {
                Left = Math.Max(0, CropRect!.Value.Left),
                Right = Math.Max(0, CropRect.Value.Right),
                Top = Math.Max(0, CropRect.Value.Top),
                Bottom = Math.Max(0, CropRect.Value.Bottom)
            };

            if (CropRect.Value.Right - CropRect.Value.Left < minSize)
            {
                if (CurrentDrag == DragMode.Left || CurrentDrag == DragMode.TopLeft || CurrentDrag == DragMode.BottomLeft)
                {
                    CropRect = new SKRect
                    {
                        Left = CropRect.Value.Right - minSize,
                        Right = CropRect.Value.Right,
                        Top = CropRect.Value.Top,
                        Bottom = CropRect.Value.Bottom
                    };
                }
                else
                {
                    CropRect = new SKRect
                    {
                        Left = CropRect.Value.Right + minSize,
                        Right = CropRect.Value.Right,
                        Top = CropRect.Value.Top,
                        Bottom = CropRect.Value.Bottom
                    };
                }
            }
            if (CropRect.Value.Bottom - CropRect.Value.Top < minSize)
            {
                if (CurrentDrag == DragMode.Top || CurrentDrag == DragMode.TopLeft || CurrentDrag == DragMode.TopRight)
                {
                    CropRect = new SKRect
                    {
                        Top = CropRect.Value.Bottom - minSize,
                        Bottom = CropRect.Value.Bottom,
                        Left = CropRect.Value.Left,
                        Right = CropRect.Value.Right
                    };
                }
                else
                {
                    CropRect = new SKRect
                    {
                        Top = CropRect.Value.Bottom + minSize,
                        Bottom = CropRect.Value.Bottom,
                        Left = CropRect.Value.Left,
                        Right = CropRect.Value.Right
                    };
                }
            }
        }

        public void NormalizeCropRect()
        {
            if (CropRect!.Value.Left > CropRect.Value.Right)
            {
                var left = CropRect.Value.Left;
                CropRect = new SKRect
                {
                    Left = CropRect.Value.Right,
                    Right = left,
                    Top = CropRect.Value.Top,
                    Bottom = CropRect.Value.Bottom
                };
            }
            if (CropRect!.Value.Top > CropRect.Value.Bottom) {
                var bottom = CropRect.Value.Bottom;
                CropRect = new SKRect
                {
                    Bottom = CropRect.Value.Top,
                    Top = bottom,
                    Left = CropRect.Value.Left,
                    Right = CropRect.Value.Right
                };
            }
        }

        public SKRect TransformCropForRotation(SKRect crop, float angle, int width, int height)
        {
            Angle = (Angle % 360 + 360) % 360;
            return Angle switch
            {
                0 => crop,
                90 => new SKRect(crop.Top, width - crop.Right, crop.Bottom, width - crop.Left),
                180 => new SKRect(width - crop.Right, height - crop.Bottom , width - crop.Left, height-crop.Top),
                270 => new SKRect(height - crop.Bottom, crop.Left, height - crop.Top, crop.Right),
                _ => crop

            };
        }

        SKBitmap Rotate(SKBitmap src, float angle)
        {
            var matrix = SKMatrix.CreateRotationDegrees(angle);
            var bounds = matrix.MapRect(new SKRect(0, 0, src.Width, src.Height)).Standardized;

            var rotated = new SKBitmap((int)bounds.Width, (int)bounds.Height);

            using var canvas = new SKCanvas(rotated);

            canvas.Translate(-bounds.Left, -bounds.Top);
            canvas.Concat(ref matrix);
            canvas.DrawBitmap(src, 0, 0);

            return rotated;
        }

        public SKBitmap Crop(SKBitmap src, SKRect crop)
        {
            //SKRectI cropI = new SKRectI(
            //    (int)Math.Floor(crop.Left),
            //    (int)Math.Floor(crop.Top),
            //    (int)Math.Ceiling(crop.Right + 0.0001f),
            //    (int)Math.Ceiling(crop.Bottom)
            //);

            //cropI.Right = Math.Min(src.Width, cropI.Right);
            //cropI.Bottom = Math.Min(src.Height, cropI.Bottom);
            SKRectI cropI = SKRectI.Round(crop);


            cropI.Left = Math.Max(0, cropI.Left);
            cropI.Top = Math.Max(0, cropI.Top);
            cropI.Right = Math.Min(src.Width, cropI.Right);
            cropI.Bottom = Math.Min(src.Height, cropI.Bottom);

            var result = new SKBitmap(cropI.Width, cropI.Height);

            using var canvas = new SKCanvas(result);

            canvas.DrawBitmap(src, cropI, new SKRect(0, 0, cropI.Width, cropI.Height));
            return result;
        }

        public void CommitCrop()
        {
            // 1. Map crop to original
            float scaleX = (float)OriginalImage!.Width / ProxyImage!.Width;
            float scaleY = (float)OriginalImage.Height / ProxyImage.Height;

            var originalCrop = new SKRect(
                CropRect!.Value.Left * scaleX,
                CropRect.Value.Top * scaleY,
                CropRect.Value.Right * scaleX,
                CropRect.Value.Bottom * scaleY
            );

            // 2. Rotate original
            var rotated = Rotate(OriginalImage, Angle);

            // 3. Transform crop rect into rotated space
            var rotatedCrop = TransformCropForRotation(
                originalCrop,
                Angle,
                OriginalImage.Width,
                OriginalImage.Height);

            // 4. Crop
            var cropped = Crop(rotated, rotatedCrop);

            // 5. Replace original
            OriginalImage = cropped;

            // 6. Reset rotation
            Angle = 0;

            // 7. Rebuild proxy
            ProxyImage = CreateProxy(OriginalImage, Math.Max(ProxyImage.Width, ProxyImage.Height));

            // 8. Reset crop rect
            CropRect = new SKRect(0, 0, ProxyImage.Width, ProxyImage.Height);

            // 9. Update transform
            UpdateTransform();
        }

        public BitmapSource Render()
        {
            var proxy = ProxyImage;
            //var transform = SKMatrix.CreateRotationDegrees(Angle);

            //var rect = new SKRect(0, 0, proxy!.Width, proxy.Height);
            //CurrentTransform = SKMatrix.CreateRotationDegrees(Angle);
            UpdateTransform();
            var bounds = CurrentTransform.MapRect(new SKRect(0, 0, proxy!.Width, proxy.Height)).Standardized;
            //bounds = bounds.Standardized;
            int width = (int)Math.Ceiling(bounds.Width);
            int height = (int)Math.Ceiling(bounds.Height);
            SKBitmap bitmap = new SKBitmap(width, height);

            //UpdateTransform();
            using SKCanvas canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Black);
            canvas.Translate(-bounds.Left, -bounds.Top);
            canvas.Concat(ref CurrentTransform);

            if (CropRect == null)
            {
                CropRect = new SKRect(0, 0, width, height);
            }
            canvas.DrawBitmap(proxy, 0, 0);

            if (CropRect != null && IsCropping)
            {
                //var transformedCrop = CurrentTransform.MapRect(CropRect!.Value).Standardized;
                //transformedCrop.Offset(-bounds.Left, -bounds.Top);
                //var full = new SKRect(0, 0, bounds.Width, bounds.Height);
                DrawCropOverlay(canvas);
            }
            return GraphicsHelper.GetBitmapFromSKBitmap(bitmap!);

        }

        private void DrawCropOverlay(SKCanvas canvas)
        {
            //var transformedCrop = CurrentTransform.MapRect(CropRect!.Value).Standardized;


            var full = new SKRect(0, 0, ProxyImage!.Width, ProxyImage.Height);
            using var overlay = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 150)
            };
            // Top
            canvas.DrawRect(new SKRect(full.Left, full.Top, full.Right, CropRect!.Value.Top), overlay);

            // Bottom
            canvas.DrawRect(new SKRect(full.Left, CropRect!.Value.Bottom, full.Right, full.Bottom), overlay);

            // Left
            canvas.DrawRect(new SKRect(full.Left, CropRect!.Value.Top, CropRect!.Value.Left, CropRect!.Value.Bottom), overlay);

            // Right
            canvas.DrawRect(new SKRect(CropRect!.Value.Right, CropRect!.Value.Top, full.Right, CropRect!.Value.Bottom), overlay);
            DrawCropBorder(canvas);
            DrawCropHandles(canvas);
        }

        private void DrawCropBorder(SKCanvas canvas)
        {
            using var paint = new SKPaint
            {
                Color = SKColors.Cyan,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            canvas.DrawRect(CropRect!.Value, paint);
        }

        private void DrawCropHandles(SKCanvas canvas)
        {
            float size = 20;
            using var paint = new SKPaint
            {
                Color = SKColors.Cyan,
                Style = SKPaintStyle.Fill,
            };
            foreach (var pt in GetHandlePoints())
            {
                canvas.DrawRect(pt.X - size / 2, pt.Y - size / 2, size, size, paint);
            }
        }

        public SKPoint ScreenToImageSpace(SKPoint pt, SKMatrix transform)
        {
            SKMatrix inverse;
            if (!transform.TryInvert(out inverse))
            {
                return pt;
            }
            else
            {
                return inverse.MapPoint(pt);
            }
        }

        List<SKPoint> GetHandlePoints()
        {
            return new List<SKPoint>
            {
                new(CropRect!.Value.Left, CropRect.Value.Top),
                new(CropRect.Value.MidX, CropRect.Value.Top),
                new(CropRect.Value.Right, CropRect.Value.Top),

                new(CropRect.Value.Left, CropRect.Value.MidY),
                new(CropRect.Value.Right, CropRect.Value.MidY),

                new(CropRect.Value.Left, CropRect.Value.Bottom),
                new(CropRect.Value.MidX, CropRect.Value.Bottom),
                new(CropRect.Value.Right, CropRect.Value.Bottom),
            };
        }



        public DragMode HitTest(SKPoint p)
        {

            float left = CropRect!.Value.Left;
            float right = CropRect.Value.Right;
            float top = CropRect.Value.Top;
            float bottom = CropRect.Value.Bottom;

            float midX = (left + right) / 2f;
            float midY = (top + bottom) / 2f;

            //// helper
            //bool Near(SKPoint a) =>
            //    Math.Abs(p.X - a.X) <= size &&
            //    Math.Abs(p.Y - a.Y) <= size;
            bool Near(SKPoint a)
            {
                float size = 30f;
                float dx = p.X - a.X;
                float dy = p.Y - a.Y;
                return (dx * dx + dy * dy) <= (size * size);
            }
            // corners
            if (Near(new SKPoint(left, top))) return DragMode.TopLeft;
            if (Near(new SKPoint(right, top))) return DragMode.TopRight;
            if (Near(new SKPoint(left, bottom))) return DragMode.BottomLeft;
            if (Near(new SKPoint(right, bottom))) return DragMode.BottomRight;

            // edges
            if (Near(new SKPoint(midX, top))) return DragMode.Top;
            if (Near(new SKPoint(midX, bottom))) return DragMode.Bottom;
            if (Near(new SKPoint(left, midY))) return DragMode.Left;
            if (Near(new SKPoint(right, midY))) return DragMode.Right;

            // inside rect = move
            if (p.X >= left && p.X <= right &&
                p.Y >= top && p.Y <= bottom)
                return DragMode.Move;

            return DragMode.None;
        }

    }

}
