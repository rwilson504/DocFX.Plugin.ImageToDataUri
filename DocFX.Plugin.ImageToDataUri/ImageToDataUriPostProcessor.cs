using HtmlAgilityPack;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Plugins;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;

namespace DocFX.Plugin.DataUri
{
    [Export(nameof(ImageToDataUriPostProcessor), typeof(IPostProcessor))]
    public class ImageToDataUriPostProcessor : IPostProcessor
    {
        private int _updatedFiles;

        public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
            => metadata;

        /// <summary>
        /// Process the manifest to update the image tags to use data-uri instead of the image source
        /// </summary>
        /// <param name="manifest"></param>
        /// <param name="outputFolder"></param>
        /// <returns></returns>
        public Manifest Process(Manifest manifest, string outputFolder)
        {
            var taskQueue = new ConcurrentBag<Task>();

            foreach (var manifestItem in manifest.Files.Where(x => x.DocumentType == "Conceptual" && !string.IsNullOrEmpty(x.SourceRelativePath)))
            {
                taskQueue.Add(Task.Run(async () =>
                {
                    foreach (var manifestItemOutputFile in manifestItem.OutputFiles.Where(x => !string.IsNullOrEmpty(x.Value.RelativePath)))
                    {
                        var sourcePath = Path.Combine(manifest.SourceBasePath, manifestItem.SourceRelativePath);
                        var outputPath = Path.Combine(outputFolder, manifestItemOutputFile.Value.RelativePath);

                        Logger.LogVerbose($"Processing: {outputPath}");
                        await UpdateImageTagsAsync(outputPath, manifest);
                    }
                }));
            }

            Task.WaitAll(taskQueue.ToArray());

            Logger.LogInfo($"Updated {_updatedFiles} files");
            return manifest;
        }

        /// <summary>
        /// Updates the image tags within an html file to use data-uri instead of the image source
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="manifest"></param>
        private async Task UpdateImageTagsAsync(string outputPath, Manifest manifest)
        {
            //convert the file to an html document
            var htmlDoc = new HtmlDocument();
            htmlDoc.Load(outputPath);

            foreach (HtmlNode link in htmlDoc.DocumentNode.SelectNodes("//img"))
            {
                HtmlAttribute att = link.Attributes["src"];
                var imageSource = att.Value;

                //if the image source is a url, skip it
                if (imageSource.StartsWith("http"))
                {
                    continue;
                }

                //check to see if the image source is a resource in the manifest
                var resource = manifest.Files.Where(x => x.SourceRelativePath == imageSource).FirstOrDefault();

                //if the image source is a resource in the manifest, get the image source from the resource
                if (resource != null)
                {
                    //get the image source path
                    var imageSourcePath = Path.Combine(Path.GetDirectoryName(outputPath), imageSource);
                    //get file contents based on source path
                    var fileContents = await File.ReadAllBytesAsync(imageSourcePath);
                    //get the mime type for the file
                    var getMimeType = GetMimeTypeForFileExtension(imageSourcePath);
                    //replace the image src with the image data-uri
                    att.Value = string.Format("data:{0};base64,{1}", getMimeType, Convert.ToBase64String(fileContents));
                }

            }
            //save the html file
            htmlDoc.Save(outputPath);
            Interlocked.Increment(ref _updatedFiles);
        }

        /// <summary>
        /// Returns the mime type of the file based on the file extension
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>        
        private string GetMimeTypeForFileExtension(string filePath)
        {
            const string DefaultContentType = "image/png";

            var provider = new FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(filePath, out string contentType))
            {
                contentType = DefaultContentType;
            }

            return contentType;
        }
    }
}