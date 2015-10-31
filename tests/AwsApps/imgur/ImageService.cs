using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using ServiceStack;
using System.Drawing;
using System.Drawing.Drawing2D;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

//Entire C# source code for ImageResizer backend - there is no other .cs :)
namespace Imgur
{
    [Route("/imgur/upload")]
    public class Upload
    {
        public string Url { get; set; }
    }

    [Route("/imgur/images")]
    public class Images { }

    [Route("/imgur/resize/{Id}")]
    public class Resize
    {
        public string Id { get; set; }
        public string Size { get; set; }
    }

    [Route("/imgur/reset")]
    public class Reset { }

    [Route("/imgur/delete/{Id}")]
    public class DeleteUpload
    {
        public string Id { get; set; }
    }

    public class ImageService : Service
    {
        const int ThumbnailSize = 100;
        readonly string UploadsDir = "imgur/uploads";
        readonly string ThumbnailsDir = "imgur/uploads/thumbnails";
        readonly List<string> ImageSizes = new[] { "320x480", "640x960", "640x1136", "768x1024", "1536x2048" }.ToList();

        public IVirtualFileSystem Files
        {
            get { return HostContext.VirtualFileSystem; }
        }

        public object Get(Images request)
        {
            return Files.GetDirectory(UploadsDir).Files.Map(x => x.Name);
        }

        public object Post(Upload request)
        {
            if (request.Url != null)
            {
                using (var ms = new MemoryStream(request.Url.GetBytesFromUrl()))
                {
                    WriteImage(ms);
                }
            }

            foreach (var uploadedFile in Request.Files.Where(uploadedFile => uploadedFile.ContentLength > 0))
            {
                using (var ms = new MemoryStream())
                {
                    uploadedFile.WriteTo(ms);
                    WriteImage(ms);
                }
            }

            return HttpResult.Redirect("/imgur/");
        }

        private void WriteImage(Stream ms)
        {
            ms.Position = 0;
            var hash = ms.ToMd5Hash();

            ms.Position = 0;
            var fileName = hash + ".png";
            using (var img = Image.FromStream(ms))
            {
                using (var msPng = MemoryStreamFactory.GetStream())
                {
                    img.Save(msPng, ImageFormat.Png);
                    msPng.Position = 0;
                    Files.WriteFile(UploadsDir.CombineWith(fileName), msPng);
                }

                var stream = Resize(img, ThumbnailSize, ThumbnailSize);
                Files.WriteFile(ThumbnailsDir.CombineWith(fileName), stream);

                ImageSizes.ForEach(x => Files.WriteFile(
                    UploadsDir.CombineWith(x).CombineWith(hash + ".png"),
                    Get(new Resize { Id = hash, Size = x }).ReadFully()));
            }
        }

        [AddHeader(ContentType = "image/png")]
        public Stream Get(Resize request)
        {
            var imageFile = Files.GetFile(UploadsDir.CombineWith(request.Id + ".png"));
            if (request.Id == null || imageFile == null)
                throw HttpError.NotFound(request.Id + " was not found");

            using (var stream = imageFile.OpenRead())
            using (var img = Image.FromStream(stream))
            {
                var parts = request.Size == null ? null : request.Size.Split('x');
                int width = img.Width;
                int height = img.Height;

                if (parts != null && parts.Length > 0)
                    int.TryParse(parts[0], out width);

                if (parts != null && parts.Length > 1)
                    int.TryParse(parts[1], out height);

                return Resize(img, width, height);
            }
        }

        public static Stream Resize(Image img, int newWidth, int newHeight)
        {
            if (newWidth != img.Width || newHeight != img.Height)
            {
                var ratioX = (double)newWidth / img.Width;
                var ratioY = (double)newHeight / img.Height;
                var ratio = Math.Max(ratioX, ratioY);
                var width = (int)(img.Width * ratio);
                var height = (int)(img.Height * ratio);

                var newImage = new Bitmap(width, height);
                Graphics.FromImage(newImage).DrawImage(img, 0, 0, width, height);
                img = newImage;

                if (img.Width != newWidth || img.Height != newHeight)
                {
                    var startX = (Math.Max(img.Width, newWidth) - Math.Min(img.Width, newWidth)) / 2;
                    var startY = (Math.Max(img.Height, newHeight) - Math.Min(img.Height, newHeight)) / 2;
                    img = Crop(img, newWidth, newHeight, startX, startY);
                }
            }

            var ms = new MemoryStream();
            img.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            return ms;
        }

        public static Image Crop(Image Image, int newWidth, int newHeight, int startX = 0, int startY = 0)
        {
            if (Image.Height < newHeight)
                newHeight = Image.Height;

            if (Image.Width < newWidth)
                newWidth = Image.Width;

            using (var bmp = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb))
            {
                bmp.SetResolution(72, 72);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(Image, new Rectangle(0, 0, newWidth, newHeight), startX, startY, newWidth, newHeight, GraphicsUnit.Pixel);

                    var ms = new MemoryStream();
                    bmp.Save(ms, ImageFormat.Png);
                    Image.Dispose();
                    var outimage = Image.FromStream(ms);
                    return outimage;
                }
            }
        }

        public object Any(DeleteUpload request)
        {
            var file = request.Id + ".png";
            var filesToDelete = new[] { UploadsDir.CombineWith(file), ThumbnailsDir.CombineWith(file) }.ToList();
            ImageSizes.Each(x => filesToDelete.Add(UploadsDir.CombineWith(x, file)));
            Files.DeleteFiles(filesToDelete);

            return HttpResult.Redirect("/imgur/");
        }

        public object Any(Reset request)
        {
            Files.DeleteFiles(Files.GetDirectory(UploadsDir).GetAllMatchingFiles("*.png"));
            File.ReadAllLines("~/imgur/preset-urls.txt".MapHostAbsolutePath()).ToList()
                .ForEach(url => WriteImage(new MemoryStream(url.Trim().GetBytesFromUrl())));

            return HttpResult.Redirect("/imgur/");
        }
    }
}