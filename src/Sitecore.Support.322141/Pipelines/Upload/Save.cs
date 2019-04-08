// Decompiled with JetBrains decompiler
// Type: Sitecore.Pipelines.Upload.Save
// Assembly: Sitecore.Kernel, Version=11.1.0.0, Culture=neutral, PublicKeyToken=null
// MVID: F8DE3552-BE39-41F4-8D7E-04A0C08DC796
// Assembly location: C:\inetpub\wwwroot\sc902.local\bin\Sitecore.Kernel.dll

using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.SecurityModel;
using Sitecore.Web;
using Sitecore.Zip;
using Sitecore.Pipelines.Upload;
using Sitecore.Support.Resources.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Sitecore.Support.Pipelines.Upload
{
    /// <summary>Saves the uploaded files.</summary>
    public class Save : UploadProcessor
    {
        /// <summary>Runs the processor.</summary>
        /// <param name="args">The arguments.</param>
        /// <exception cref="T:System.Exception"><c>Exception</c>.</exception>
        public void Process(UploadArgs args)
        {
            Assert.ArgumentNotNull((object)args, nameof(args));
            for (int index = 0; index < args.Files.Count; ++index)
            {
                HttpPostedFile file1 = args.Files[index];
                if (!string.IsNullOrEmpty(file1.FileName))
                {
                    try
                    {
                        bool flag = UploadProcessor.IsUnpack(args, file1);
                        if (args.FileOnly)
                        {
                            if (flag)
                            {
                                Save.UnpackToFile(args, file1);
                            }
                            else
                            {
                                string file2 = this.UploadToFile(args, file1);
                                if (index == 0)
                                    args.Properties["filename"] = (object)FileHandle.GetFileHandle(file2);
                            }
                        }
                        else
                        {
                            MediaUploader mediaUploader = new MediaUploader()
                            {
                                File = file1,
                                Unpack = flag,
                                Folder = args.Folder,
                                Versioned = args.Versioned,
                                Language = args.Language,
                                AlternateText = args.GetFileParameter(file1.FileName, "alt"),
                                Overwrite = args.Overwrite,
                                FileBased = args.Destination == UploadDestination.File
                            };
                            List<MediaUploadResult> mediaUploadResultList;
                            using (new SecurityDisabler())
                                mediaUploadResultList = mediaUploader.Upload();
                            Log.Audit((object)this, "Upload: {0}", new string[1]
                            {
                file1.FileName
                            });
                            foreach (MediaUploadResult mediaUploadResult in mediaUploadResultList)
                                this.ProcessItem(args, (MediaItem)mediaUploadResult.Item, mediaUploadResult.Path);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Could not save posted file: " + file1.FileName, ex, (object)this);
                        throw;
                    }
                }
            }
        }

        /// <summary>Processes the item.</summary>
        /// <param name="args">The arguments.</param>
        /// <param name="mediaItem">The media item.</param>
        /// <param name="path">The path.</param>
        private void ProcessItem(UploadArgs args, MediaItem mediaItem, string path)
        {
            Assert.ArgumentNotNull((object)args, nameof(args));
            Assert.ArgumentNotNull((object)mediaItem, nameof(mediaItem));
            Assert.ArgumentNotNull((object)path, nameof(path));
            if (args.Destination == UploadDestination.Database)
                Log.Info("Media Item has been uploaded to database: " + path, (object)this);
            else
                Log.Info("Media Item has been uploaded to file system: " + path, (object)this);
            args.UploadedItems.Add(mediaItem.InnerItem);
        }

        /// <summary>Unpacks to file.</summary>
        /// <param name="args">The arguments.</param>
        /// <param name="file">The file.</param>
        private static void UnpackToFile(UploadArgs args, HttpPostedFile file)
        {
            Assert.ArgumentNotNull((object)args, nameof(args));
            Assert.ArgumentNotNull((object)file, nameof(file));
            string filename = FileUtil.MapPath(TempFolder.GetFilename("temp.zip"));
            file.SaveAs(filename);
            using (ZipReader zipReader = new ZipReader(filename))
            {
                foreach (ZipEntry entry1 in zipReader.Entries)
                {
                    ZipEntry entry = entry1;
                    if (((IEnumerable<char>)Path.GetInvalidFileNameChars()).Any<char>((Func<char, bool>)(ch => entry.Name.Contains<char>(ch))))
                    {
                        string message = string.Format("The \"{0}\" file was not uploaded because it contains malicious file: \"{1}\"", (object)file.FileName, (object)entry.Name);
                        Log.Warn(message, (object)typeof(Save));
                        args.UiResponseHandlerEx.MaliciousFile(StringUtil.EscapeJavascriptString(file.FileName));
                        args.ErrorText = message;
                        args.AbortPipeline();
                        return;
                    }
                }
                foreach (ZipEntry entry in zipReader.Entries)
                {
                    string str = FileUtil.MakePath(args.Folder, entry.Name, '\\');
                    if (entry.IsDirectory)
                    {
                        Directory.CreateDirectory(str);
                    }
                    else
                    {
                        if (!args.Overwrite)
                            str = FileUtil.GetUniqueFilename(str);
                        Directory.CreateDirectory(Path.GetDirectoryName(str));
                        lock (FileUtil.GetFileLock(str))
                            FileUtil.CreateFile(str, entry.GetStream(), true);
                    }
                }
            }
        }

        /// <summary>Uploads to file.</summary>
        /// <param name="args">The arguments.</param>
        /// <param name="file">The file.</param>
        /// <returns>The name of the uploaded file</returns>
        private string UploadToFile(UploadArgs args, HttpPostedFile file)
        {
            Assert.ArgumentNotNull((object)args, nameof(args));
            Assert.ArgumentNotNull((object)file, nameof(file));
            string str = FileUtil.MakePath(args.Folder, Path.GetFileName(file.FileName), '\\');
            if (!args.Overwrite)
                str = FileUtil.GetUniqueFilename(str);
            file.SaveAs(str);
            Log.Info("File has been uploaded: " + str, (object)this);
            return Assert.ResultNotNull<string>(str);
        }
    }
}
