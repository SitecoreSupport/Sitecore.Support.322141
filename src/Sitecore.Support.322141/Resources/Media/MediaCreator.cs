﻿// Sitecore.Resources.Media.MediaCreator
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.IO;
using Sitecore.Pipelines.GetMediaCreatorOptions;
using Sitecore.Resources.Media;
using Sitecore.SecurityModel;
using System;
using System.IO;

namespace Sitecore.Support.Resources.Media
{
    /// <summary>
    /// MediaCreator class
    /// </summary>
    public class MediaCreator
    {
        /// <summary>
        /// The default site.
        /// </summary>
        protected const string DefaultSite = "shell";

        /// <summary>
        /// Gets the file based stream path.
        /// </summary>
        /// <param name="itemPath">The item path.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="options">The options.</param>
        /// <returns>The get file based stream path.</returns>
        public static string GetFileBasedStreamPath(string itemPath, string filePath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull(itemPath, "itemPath");
            Assert.ArgumentNotNull(filePath, "filePath");
            Assert.ArgumentNotNull(options, "options");
            return MediaCreator.GetOutputFilePath(itemPath, filePath, options);
        }

        /// <summary>
        /// Attachs new stream to exists MediaItem and changes the FilePath
        /// </summary>
        /// <param name="stream">
        /// The new file stream
        /// </param>
        /// <param name="itemPath">
        /// Full item patch
        /// </param>
        /// <param name="fileName">
        /// Filename with extension
        /// </param>
        /// <param name="options">
        /// The MediaCreator's Options 
        /// </param>
        /// <returns>
        /// The Media Item
        /// </returns>
        public virtual Item AttachStreamToMediaItem(Stream stream, string itemPath, string fileName, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull(stream, "stream");
            Assert.ArgumentNotNullOrEmpty(fileName, "fileName");
            Assert.ArgumentNotNull(options, "options");
            Assert.ArgumentNotNull(itemPath, "itemPath");
            Item item = this.CreateItem(itemPath, fileName, options);
            Sitecore.Resources.Media.Media media = MediaManager.GetMedia(item);
            media.SetStream(stream, FileUtil.GetExtension(fileName));
            return media.MediaData.MediaItem;
        }

        /// <summary>
        /// Creates a new media item from a file.
        /// </summary>
        /// <param name="filePath">
        /// The file path.
        /// </param>
        /// <param name="options">
        /// The options.
        /// </param>
        /// <returns>
        /// The Media Item.
        /// </returns>
        public virtual MediaItem CreateFromFile(string filePath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNullOrEmpty(filePath, "filePath");
            Assert.ArgumentNotNull(options, "options");
            string path = FileUtil.MapPath(filePath);
            using (new SecurityDisabler())
            {
                using (FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return this.CreateFromStream(fileStream, filePath, options);
                }
            }
        }

        /// <summary>
        /// Creates a media folder from a file system folder.
        /// </summary>
        /// <param name="folderPath">
        /// The folder path.
        /// </param>
        /// <param name="options">
        /// The options.
        /// </param>
        /// <returns>
        /// The Sitecore item.
        /// </returns>
        public virtual Item CreateFromFolder(string folderPath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNullOrEmpty(folderPath, "folderPath");
            Assert.ArgumentNotNull(options, "options");
            string itemPath = this.GetItemPath(folderPath, options);
            return this.CreateFolder(itemPath, options);
        }

        /// <summary>
        /// Creates the media from a file.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="options">The options.</param>
        /// <returns>The Created Item.</returns>
        public virtual Item CreateFromStream(Stream stream, string filePath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull(stream, "stream");
            Assert.ArgumentNotNull(filePath, "filePath");
            Assert.ArgumentNotNull(options, "options");
            return this.CreateFromStream(stream, filePath, true, options);
        }

        /// <summary>
        /// Creates the media from a file.
        /// </summary>
        /// <param name="stream">
        /// The stream.
        /// </param>
        /// <param name="filePath">
        /// The file path.
        /// </param>
        /// <param name="setStreamIfEmpty">
        /// if set to <c>true</c> [set stream if empty].
        /// </param>
        /// <param name="options">
        /// The options.
        /// </param>
        /// <returns>
        /// The Created Item.
        /// </returns>
        public virtual Item CreateFromStream(Stream stream, string filePath, bool setStreamIfEmpty, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull(stream, "stream");
            Assert.ArgumentNotNullOrEmpty(filePath, "filePath");
            Assert.ArgumentNotNull(options, "options");
            string itemPath = this.GetItemPath(filePath, options);
            return this.AttachStreamToMediaItem(stream, itemPath, filePath, options);
        }

        /// <summary>
        /// A new file has been created.
        /// </summary>
        /// <param name="filePath">
        /// The full path to the file.
        /// </param>
        public virtual void FileCreated(string filePath)
        {
            Assert.ArgumentNotNullOrEmpty(filePath, "filePath");
            this.SetContext();
            lock (FileUtil.GetFileLock(filePath))
            {
                if (FileUtil.IsFolder(filePath))
                {
                    MediaCreatorOptions empty = MediaCreatorOptions.Empty;
                    empty.Build(GetMediaCreatorOptionsArgs.FileBasedContext);
                    this.CreateFromFolder(filePath, empty);
                }
                else
                {
                    MediaCreatorOptions empty2 = MediaCreatorOptions.Empty;
                    long length = new FileInfo(filePath).Length;
                    empty2.FileBased = (length > Settings.Media.MaxSizeInDatabase || Settings.Media.UploadAsFiles);
                    empty2.Build(GetMediaCreatorOptionsArgs.FileBasedContext);
                    this.CreateFromFile(filePath, empty2);
                }
            }
        }

        /// <summary>
        /// A file has been deleted.
        /// </summary>
        /// <param name="filePath">
        /// The full path to the file.
        /// </param>
        public virtual void FileDeleted(string filePath)
        {
            Assert.ArgumentNotNullOrEmpty(filePath, "filePath");
        }

        /// <summary>
        /// A file has been renamed.
        /// </summary>
        /// <param name="filePath">
        /// The path to the file.
        /// </param>
        /// <param name="oldFilePath">
        /// The old path to the file.
        /// </param>
        public virtual void FileRenamed(string filePath, string oldFilePath)
        {
            Assert.ArgumentNotNullOrEmpty(filePath, "filePath");
            Assert.ArgumentNotNullOrEmpty(oldFilePath, "oldFilePath");
            this.SetContext();
            lock (FileUtil.GetFileLock(filePath))
            {
                MediaCreatorOptions empty = MediaCreatorOptions.Empty;
                empty.Build(GetMediaCreatorOptionsArgs.FileBasedContext);
                string itemPath = this.GetItemPath(oldFilePath, empty);
                Database database = this.GetDatabase(empty);
                Item obj = database.GetItem(itemPath);
                if (obj != null)
                {
                    string itemPath2 = this.GetItemPath(filePath, empty);
                    string fileName = FileUtil.GetFileName(itemPath2);
                    string extension = FileUtil.GetExtension(filePath);
                    using (new EditContext(obj, SecurityCheck.Disable))
                    {
                        obj.Name = fileName;
                        ((BaseItem)obj)["extension"] = extension;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the item path corresponding to a file.
        /// </summary>
        /// <param name="filePath">
        /// The file path.
        /// </param>
        /// <param name="options">
        /// The options.
        /// </param>
        /// <returns>
        /// The item path.
        /// </returns>
        public virtual string GetItemPath(string filePath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull(filePath, "filePath");
            Assert.ArgumentNotNull(options, "options");
            if (!string.IsNullOrEmpty(options.Destination))
            {
                return options.Destination;
            }
            string text = FileUtil.SubtractPath(filePath, Settings.MediaFolder);
            Assert.IsNotNull(text, typeof(string), "File based media must be located beneath the media folder: '{0}'. Current file: {1}", Settings.MediaFolder, filePath);
            int length = text.LastIndexOf('.');
            if (length < text.LastIndexOf('\\'))
            {
                length = -1;
            }
            bool alwaysKeepExtension = FileUtil.IsFolder(filePath);
            if (length >= 0 && !alwaysKeepExtension)
            {
                string str = string.Empty;
                if (options.IncludeExtensionInItemName)
                {
                    str = Settings.Media.WhitespaceReplacement + StringUtil.Mid(text, length + 1).ToLowerInvariant();
                }
                text = StringUtil.Left(text, length) + str;
            }
            Assert.IsNotNullOrEmpty(text, "The relative path of a media to create is empty. Original file path: '{0}'.", filePath);
            string path = FileUtil.MakePath("/sitecore/media library", text.Replace('\\', '/'));
            path = MediaPathManager.ProposeValidMediaPath(path, alwaysKeepExtension);
            return Assert.ResultNotNull(path);
        }

        /// <summary>
        /// Gets private folder for media blob inside the Media.FileFolder.
        /// </summary>
        /// <param name="itemID">
        /// The item ID.
        /// </param>
        /// <param name="fullPath">
        /// The full path.
        /// </param>
        /// <returns>
        /// The short folder.
        /// </returns>
        public virtual string GetMediaStorageFolder(ID itemID, string fullPath)
        {
            Assert.IsNotNull(itemID, "itemID is null");
            Assert.IsNotNullOrEmpty(fullPath, "fullPath is empty");
            string fileName = FileUtil.GetFileName(fullPath);
            string str = itemID.ToString();
            return string.Format("/{0}/{1}/{2}/{3}{4}", str[1], str[2], str[3], str, fileName);
        }

        /// <summary>
        /// Creates a media folder in the content database.
        /// </summary>
        /// <param name="itemPath">
        /// The item path.
        /// </param>
        /// <param name="options">
        /// The options.
        /// </param>
        /// <returns>
        /// The Created Folder.
        /// </returns>
        protected virtual Item CreateFolder(string itemPath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNullOrEmpty(itemPath, "itemPath");
            Assert.ArgumentNotNull(options, "options");
            using (new SecurityDisabler())
            {
                using (new LanguageSwitcher(options.Language))
                {
                    TemplateItem folderTemplate = this.GetFolderTemplate(options);

                    Database database = this.GetDatabase(options);
                    Item obj = database.GetItem(itemPath, options.Language);
                    if (obj != null)
                    {
                        return obj;
                    }
                    Item itemPath2 = database.CreateItemPath(itemPath, folderTemplate, folderTemplate);
                    Assert.IsNotNull(itemPath2, typeof(Item), "Could not create media folder: '{0}'.", itemPath);

                    return itemPath2;
                }
            }
        }

        /// <summary>
        /// Creates a media item in the content database.
        /// </summary>
        /// <param name="itemPath">
        /// The item path.
        /// </param>
        /// <param name="filePath">
        /// The file path.
        /// </param>
        /// <param name="options">
        /// The options.
        /// </param>
        /// <returns>
        /// The Created Item.
        /// </returns>
        protected virtual Item CreateItem(string itemPath, string filePath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNullOrEmpty(itemPath, "itemPath");
            Assert.ArgumentNotNullOrEmpty(filePath, "filePath");
            Assert.ArgumentNotNull(options, "options");
            Item obj = default(Item);
            using (new SecurityDisabler())
            {
                Database database = this.GetDatabase(options);
                Item obj2 = options.OverwriteExisting ? database.GetItem(itemPath, options.Language) : null;
                Item parentFolder = this.GetParentFolder(itemPath, options);
                string itemName = this.GetItemName(itemPath);
                if (obj2 != null && !obj2.HasChildren && obj2.TemplateID != TemplateIDs.MediaFolder)
                {
                    obj = obj2;
                    obj.Versions.RemoveAll(true);
                    obj = obj.Database.GetItem(obj.ID, obj.Language, Sitecore.Data.Version.Latest);
                    Assert.IsNotNull(obj, "item");
                    obj.Editing.BeginEdit();
                    foreach (Field field in obj.Fields)
                    {
                        field.Reset();
                    }
                    obj.Editing.EndEdit();
                    obj.Editing.BeginEdit();
                    obj.Name = itemName;
                    obj.TemplateID = this.GetItemTemplate(filePath, options).ID;
                    obj.Editing.EndEdit();
                }
                else
                {
                    obj = parentFolder.Add(itemName, this.GetItemTemplate(filePath, options));
                }
                Assert.IsNotNull(obj, typeof(Item), "Could not create media item: '{0}'.", itemPath);
                Language[] itemMediaLanguages = this.GetItemMediaLanguages(options, obj);
                string extension = FileUtil.GetExtension(filePath);
                Language[] array = itemMediaLanguages;
                foreach (Language language in array)
                {
                    MediaItem mediaItem = obj.Database.GetItem(obj.ID, language, Sitecore.Data.Version.Latest);
                    if (mediaItem != null)
                    {
                        using (new EditContext(mediaItem, SecurityCheck.Disable))
                        {
                            mediaItem.Extension = StringUtil.GetString(mediaItem.Extension, extension);
                            mediaItem.FilePath = this.GetFullFilePath(obj.ID, filePath, itemPath, options);
                            mediaItem.Alt = StringUtil.GetString(mediaItem.Alt, options.AlternateText);
                            mediaItem.InnerItem.Statistics.UpdateRevision();
                        }
                    }
                }
            }
            obj.Reload();
            return obj;
        }

        /// <summary>
        /// Gets the languages to create item in.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="item">The item to have language versions initialized.</param>
        /// <returns>A collection of languages to create item language versions in.</returns>
        protected virtual Language[] GetItemMediaLanguages(MediaCreatorOptions options, Item item)
        {
            Assert.ArgumentNotNull(options, "options");
            Assert.ArgumentNotNull(item, "item");
            Assert.Required(item.Database, "item.Database");
            //if (!options.Versioned)
            //{
            //    return item.Database.Languages;
            //}
            return new Language[1]
            {
                item.Language
            };
        }

        /// <summary>
        /// Creates full path for mediaItem to the Media storage 
        /// </summary>
        /// <param name="itemID">
        /// The Item's ID
        /// </param>
        /// <param name="fileName">
        /// The file path
        /// </param>
        /// <param name="itemPath">
        /// The item path
        /// </param>
        /// <param name="options">
        /// The options
        /// </param>
        /// <returns>
        /// The full file path
        /// </returns>
        protected virtual string GetFullFilePath(ID itemID, string fileName, string itemPath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull(itemID, "itemID");
            Assert.ArgumentNotNull(fileName, "fileName");
            Assert.ArgumentNotNull(itemPath, "itemPath");
            Assert.ArgumentNotNull(options, "options");
            return MediaCreator.GetOutputFilePath(itemPath, this.GetMediaStorageFolder(itemID, fileName), options);
        }

        /// <summary>
        /// Gets the output file path.
        /// </summary>
        /// <param name="itemPath">
        /// The item path.
        /// </param>
        /// <param name="filePath">
        /// The file path.
        /// </param>
        /// <param name="options">
        /// The options.
        /// </param>
        /// <returns>
        /// The get output file path.
        /// </returns>
        private static string GetOutputFilePath(string itemPath, string filePath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull(itemPath, "itemPath");
            Assert.ArgumentNotNull(filePath, "filePath");
            Assert.ArgumentNotNull(options, "options");
            if (!options.FileBased)
            {
                return string.Empty;
            }
            if (!string.IsNullOrEmpty(options.OutputFilePath))
            {
                return options.OutputFilePath;
            }
            string extension = FileUtil.GetExtension(filePath);
            string str = FileUtil.GetFileName(filePath);
            if (extension.Length > 0)
            {
                str = str.Substring(0, str.Length - extension.Length - 1);
            }
            string itemPath2 = string.Format("{0}/{1}", FileUtil.GetParentPath(filePath), str);
            return MediaPathManager.GetMediaFilePath(itemPath2, extension);
        }

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <param name="options">
        /// The options.
        /// </param>
        /// <returns>
        /// The Database.
        /// </returns>
        private Database GetDatabase(MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull(options, "options");
            return Assert.ResultNotNull(options.Database ?? Context.ContentDatabase ?? Context.Database);
        }

        /// <summary>
        /// Gets the template for a media folder.
        /// </summary>
        /// <param name="options">
        /// The options.
        /// </param>
        /// <returns>
        /// The Template Item.
        /// </returns>
        private TemplateItem GetFolderTemplate(MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull(options, "options");
            Database database = this.GetDatabase(options);
            TemplateItem template = database.Templates[TemplateIDs.MediaFolder];
            Assert.IsNotNull(template, typeof(TemplateItem), "Could not find folder template for media. Template: '{0}'", TemplateIDs.MediaFolder.ToString());
            return template;
        }

        /// <summary>
        /// Gets the name of the item from a path.
        /// </summary>
        /// <param name="itemPath">
        /// The item path.
        /// </param>
        /// <returns>
        /// The get item name.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// <c>InvalidOperationException</c>.
        /// </exception>
        private string GetItemName(string itemPath)
        {
            Assert.ArgumentNotNull(itemPath, "itemPath");
            string lastPart = StringUtil.GetLastPart(itemPath, '/', string.Empty);
            if (string.IsNullOrEmpty(lastPart))
            {
                if (!Settings.Media.IncludeExtensionsInItemNames)
                {
                    return "unnamed";
                }
                throw new InvalidOperationException("Invalid item path for media item: " + itemPath);
            }
            return lastPart;
        }

        /// <summary>
        /// Gets the template for a media item.
        /// </summary>
        /// <param name="filePath">
        /// The file path of the media.
        /// </param>
        /// <param name="options">
        /// The options.
        /// </param>
        /// <returns>
        /// The Template Item.
        /// </returns>
        private TemplateItem GetItemTemplate(string filePath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull(filePath, "filePath");
            Assert.ArgumentNotNull(options, "options");
            string extension = FileUtil.GetExtension(filePath);
            string template3 = MediaManager.Config.GetTemplate(extension, options.Versioned);
            Assert.IsNotNullOrEmpty(template3, "Could not find template for extension '{0}' (versioned: {1}).", extension, options.Versioned);
            Database database = this.GetDatabase(options);
            TemplateItem template2 = database.Templates[template3];
            Assert.IsNotNull(template2, typeof(TemplateItem), "Could not find item template for media. Template: '{0}'", template3);
            return template2;
        }

        /// <summary>
        /// Gets the parent folder from an item path.
        /// </summary>
        /// <param name="itemPath">
        /// The item path.
        /// </param>
        /// <param name="options">
        /// The options.
        /// </param>
        /// <returns>
        /// The parent folder.
        /// </returns>
        private Item GetParentFolder(string itemPath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull(itemPath, "itemPath");
            Assert.ArgumentNotNull(options, "options");
            string[] strArray = StringUtil.Divide(itemPath, '/', true);
            string itemPath2 = (strArray.Length > 1) ? strArray[0] : "/sitecore/media library";
            return this.CreateFolder(itemPath2, options);
        }

        /// <summary>
        /// Sets the context (if it has not been set).
        /// </summary>
        private void SetContext()
        {
            if (Context.Site == null)
            {
                Context.SetActiveSite("shell");
            }
        }
    }
}