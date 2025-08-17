using GrowJo.Helpers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace GrowJo
{
    public class DisplayProjectData : ProjectData
    {
        public BitmapSource? ProjectThumbnail { get; set; }

        public DisplayProjectData()
        {

        }

        public DisplayProjectData(ProjectData source)
        {
            Medium = source.Medium;
            CustomMedium = source.CustomMedium;
            StrainName = source.StrainName;
            ProjectThumbnailFilename = source.ProjectThumbnailFilename;
            Entries = source.Entries;
            FinalYield = source.FinalYield;
            Potency = source.Potency;
            Terpenes = source.Terpenes;
            Filename = source.Filename;
        }

        public void LoadThumbnail()
        {
            SKBitmap loadedBitmap;
            if (!string.IsNullOrWhiteSpace(ProjectThumbnailFilename) && File.Exists(ProjectThumbnailFilename))
            {
                loadedBitmap = GraphicsHelper.LoadBitmapFromFile(ProjectThumbnailFilename);
                loadedBitmap = loadedBitmap.Resize(new SKImageInfo(256, 256), SKFilterQuality.None);
            }
            else
            {
                loadedBitmap = GraphicsHelper.LoadBitmapFromFile($"{AppDomain.CurrentDomain.BaseDirectory}Content\\NoImage.png");
            }
            ProjectThumbnail = GraphicsHelper.GetBitmapFromSKBitmap(loadedBitmap);
        }
    }

    public class ProjectData : EventArgs
    {
        public GrowMedium? Medium { get; set; }
        public string? CustomMedium { get; set; }
        public string? StrainName { get; set; }
        public string? ProjectThumbnailFilename { get; set; }
        public Dictionary<DateTime, DailyEntry>? Entries { get; set; }
        public float? FinalYield { get; set; }
        public float? Potency { get; set; }
        public Dictionary<string, float>? Terpenes { get; set; }
        public string? Filename { get; set; }
    }

    public class DailyEntry
    {
        //public DateTime Date { get; set; }
        public List<Actions>? Actions { get; set; }
        public List<string>? CustomActions { get; set; }
        public string? Notes { get; set; }
        public List<string>? PictureFilenames { get; set; }
        public Stage State { get; set; }
        public List<NutrientData>? NutrientData { get; set; }
    }

    public class DailyEntrySelectedActions
    {
        public Actions? Action { get; set; }
        public bool IsCustom { get; set; }
        public string? CustomAction { get; set; }
        public string ActionText { get
            {
                if (IsCustom)
                {
                    return CustomAction!;
                }
                else
                {
                    return Action.ToString()!;
                }
            } 
        }
    }

    public class ImageData
    {
        public string Filename { get; set; } = string.Empty;
        public BitmapSource? Thumbnail { get; set; }
        public BitmapSource? WholeImage { get; set; }

        public ImageData() { }
        public ImageData(string filename)
        {
            Filename = filename;
            if (File.Exists(Filename))
            {
                SKBitmap ogImage = GraphicsHelper.LoadBitmapFromFile(Filename);
                if (ogImage != null)
                {
                    WholeImage = GraphicsHelper.GetBitmapFromSKBitmap(ogImage);
                    var resized = ogImage.Resize(new SKImageInfo(256, 256), SKFilterQuality.None);
                    Thumbnail = GraphicsHelper.GetBitmapFromSKBitmap(resized);
                }
            }
        }
    }

    public class NutrientData
    {
        public float Amount { get; set; }
        public MeasurementUnits Unit { get; set; }
        public string NutrientName { get; set; } = string.Empty;
        public string NutrientDescription { get
            {
                return $"{Amount} {Unit.ToString()} {NutrientName}";
            } }
    }

    public interface IImageCmd
    {
        SKBitmap? Do(SKBitmap source);
    }

    public class ImageCmdRotate : IImageCmd
    {
        public float Angle { get; private set; }
        public ImageCmdRotate(float angle)
        {
            Angle = angle;
        }
        
        public SKBitmap? Do(SKBitmap source)
        {
            return GraphicsHelper.Rotate(source, Angle);
        }
    }

    public class ImageCmdResize : IImageCmd
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public ImageCmdResize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public SKBitmap? Do(SKBitmap source)
        {
            return source.Resize(new SKImageInfo(Width, Height), SKFilterQuality.None);
        }
    }

    public class ImageCmdCrop : IImageCmd
    {
        public SKRect? Rect { get; private set; }
        public ImageCmdCrop(SKRect? rect)
        {
            Rect = rect;
        }

        public SKBitmap? Do(SKBitmap source)
        {
            //Rect needs to compensate for original image size.
            SKImageInfo imageInfo = new SKImageInfo((int)Rect!.Value.Width, (int)Rect.Value.Height);
            using (SKSurface surface = SKSurface.Create(imageInfo))
            {
                SKCanvas canvas = surface.Canvas;
                SKRect SourceRect = Rect.Value;
                SKRect DestRect = new SKRect(0, 0, Rect.Value.Width, Rect.Value.Height);
                canvas.DrawBitmap(source, SourceRect, DestRect);
                using (SKImage datImage = surface.Snapshot())
                using (SKData data = datImage.Encode(SKEncodedImageFormat.Png, 100))
                {
                    return SKBitmap.Decode(data);
                }
            }
        }
    }

    public enum Stage
    {
        Germination,
        Seedling,
        Veg,
        Flower,
        Drying,
        Curing,
        Done
    }

    public enum Actions
    {
        ChangeNutrients,
        Water,
        FlippedLights,
        HST,
        LST,
        Defoliation
    }

    public enum MeasurementUnits
    {
        Tsp,
        Tbsp,
        Cup,
        Milligram,
        Gram,
        Ounce,
        Pound,
        Kilo
    }

    public enum GrowMedium
    {
        DWC,
        Coco,
        Soil,
        LivingSoil,
        Other = 99
    }

}
