using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using log4net;
using CoreSystem.Util;
using System.Text.RegularExpressions;

namespace LogRotator
{
    public class Pattern
    {
        #region XML tags

        internal const string XML_PATTERN = "pattern";

        private const string XML_ACTION = "action";

        private const string XML_DIR_PATH = "dirPath";

        private const string XML_FILE_PATTERN = "filePattern";

        private const string XML_OFFSET = "offset";

        private const string XML_SUB_DIRS = "subDirs";

        private const string XML_DELETE_UNCOMPRESSED = "deleteUnCompressed";

        private const string XML_SIZE = "minSize";

        private const string XML_DELETE_SUB_DIRS = "deleteSubDirs";

        #endregion

        private static readonly string AssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private static readonly ILog Logger = LogManager.GetLogger(typeof(Pattern));

        private DirectoryInfo dirInfo;

        public PatternAction Action { get; set; }

        public string DirPath { get; set; }

        public string FilePattern { get; set; }

        public TimeSpan Offset { get; set; }

        public bool SubDirs { get; set; }

        public bool DeleteUnCompressed { get; set; }

        public long Size { get; set; }

        public bool DeleteSubDirs { get; set; }

        public DirectoryInfo DirInfo
        {
            get
            {
                if (this.dirInfo == null)
                    this.dirInfo = Path.IsPathRooted(this.DirPath) ? new DirectoryInfo(this.DirPath) : new DirectoryInfo(Path.Combine(AssemblyDir, this.DirPath));

                return this.dirInfo;
            }
        }

        public FileInfo[] GetFiles()
        {
            if (this.DirInfo.Exists)
            {
                var time = DateTime.Now - this.Offset;
                return this.DirInfo.GetFiles(this.FilePattern, this.SubDirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                            .Where(f => f.LastWriteTime < time && f.Length >= this.Size)
                            .ToArray();
            }
            else
                Logger.WarnFormat(string.Format("Directory '{0}' doesn't exists", this.DirInfo.FullName));

            return new FileInfo[] { };
        }

        public IEnumerable<FileInfo> GetFiles(DirectoryInfo directoryInfo)
        {
            if (directoryInfo.Exists)
            {
                var duration = DateTime.Now - this.Offset;
                foreach (var fileInfo in directoryInfo.EnumerateFiles(this.FilePattern, SearchOption.TopDirectoryOnly))
                    if (fileInfo.LastWriteTime < duration && fileInfo.Length >= this.Size)
                        yield return fileInfo;

                if (this.SubDirs)
                {
                    var subDirs = directoryInfo.EnumerateDirectories()
                                               .Where(d => d.CreationTime < duration)
                                               .OrderBy(d => d.CreationTime);
                    foreach (var dirInfo in subDirs)
                        foreach (var fileInfo in this.GetFiles(dirInfo))
                            yield return fileInfo;
                }
            }
        }

        private IEnumerable<DirectoryInfo> GetSubDirectories(DirectoryInfo directoryInfo)
        {
            if (directoryInfo.Exists && directoryInfo.CreationTime < (DateTime.Now - this.Offset))
            {
                foreach (var subDirInfo in directoryInfo.EnumerateDirectories().OrderBy(d => d.CreationTime))
                    foreach (var deleteDirInfo in this.GetSubDirectories(subDirInfo))
                        yield return deleteDirInfo;

                if (this.DirInfo != directoryInfo && !directoryInfo.EnumerateFileSystemInfos().Any())
                    yield return directoryInfo;
            }
        }

        public int Do(int maxBatchSize)
        {
            Logger.InfoFormat("Processing Pattern {0}", this);
            switch (this.Action)
            {
                case PatternAction.Rotate:
                    var rotateFiles = this.GetFiles(this.DirInfo).Take(maxBatchSize).ToArray();
                    foreach (var fileInfo in rotateFiles)
                    {
                        Logger.InfoFormat("Compressing file '{0}'", fileInfo.FullName);
                        try
                        {
                            var compressFilePath = GunZip.Compress(fileInfo.FullName);

                            Logger.InfoFormat("Deleting original file '{0}'", fileInfo.FullName);
                            try { fileInfo.Delete(); }
                            catch (Exception ex)
                            {
                                Logger.Error("Failed to delete file", ex);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Failed to compress file", ex);
                        }
                    }
                    return rotateFiles.Length;
                case PatternAction.Delete:
                    var deleteFiles = this.GetFiles(this.DirInfo).Take(maxBatchSize).ToArray();
                    foreach (var fileInfo in deleteFiles)
                    {
                        Logger.InfoFormat("Deleting file '{0}'", fileInfo.FullName);
                        try
                        {
                            if (fileInfo.Extension != ".gz" && !this.DeleteUnCompressed)
                                throw new InvalidOperationException(string.Format("Cannot delete uncompressed file '{0}' (without extension .gz) unless deleteUnCompressed attribute is explicitly set to true in LogRotator configuration file", fileInfo.Name));

                            fileInfo.Delete();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Failed to delete file", ex);
                        }
                    }

                    if (this.DeleteSubDirs)
                    {
                        var subDirectories = deleteFiles.Length == 0
                            ? this.GetSubDirectories(this.DirInfo).Take(maxBatchSize)
                            : deleteFiles.Select(f => f.DirectoryName)
                                         .Distinct()
                                         .Select(d => new DirectoryInfo(d))
                                         .Where(d => d.Exists && !d.EnumerateFileSystemInfos().Any());

                        foreach (var deleteDirInfo in subDirectories)
                        {
                            Logger.InfoFormat("Deleting directory '{0}'", deleteDirInfo.FullName);
                            try
                            {
                                deleteDirInfo.Delete();
                            }
                            catch (Exception ex)
                            {
                                Logger.Warn("Unable to delete directory: " + deleteDirInfo.FullName, ex);
                            }
                        }
                    }

                    return deleteFiles.Length;
                default:
                    throw new NotSupportedException(string.Format("Pattern Action '{0}' not supported/unknown", this.Action));
            }
        }

        public override string ToString()
        {
            return string.Format("[Action: {0}, DirPath: {1}, FilePattern: {2}, Offset: {3:c}, SubDirs: {4}, DeleteUnCompressed: {5}, minSize: {6}, deleteSubDirs: {7}]", this.Action, this.DirPath, this.FilePattern, this.Offset, this.SubDirs, this.DeleteUnCompressed, this.Size, this.DeleteSubDirs);
        }

        internal static Pattern Parse(XElement pattern)
        {
            if (pattern.Name != XML_PATTERN)
                throw new ArgumentException(string.Format("Element node name is not pattern: '{0}'", pattern.Name));

            var action = (string)pattern.GetMandatoryAttribute(XML_ACTION);
            var dirPath = (string)pattern.GetMandatoryAttribute(XML_DIR_PATH);
            var filePattern = (string)pattern.GetMandatoryAttribute(XML_FILE_PATTERN);
            var offset = (string)pattern.GetMandatoryAttribute(XML_OFFSET);
            var subDirs = (bool?)pattern.Attribute(XML_SUB_DIRS);
            var deleteUnCompressed = (bool?)pattern.Attribute(XML_DELETE_UNCOMPRESSED);
            var size = (long?)pattern.Attribute(XML_SIZE);
            var deleteSubDirs = (bool?)pattern.Attribute(XML_DELETE_SUB_DIRS);


            PatternAction patterAction;
            if (!Enum.TryParse<PatternAction>(action, true, out patterAction))
                throw new InvalidOperationException(string.Format("Invalid action value: '{0}' at pattern: '{1}', valid values are: ", action, string.Join(", ", Enum.GetNames(typeof(PatternAction)))));

            TimeSpan offsetSpan;
            if (!TimeSpan.TryParse(offset, out offsetSpan))
                throw new InvalidOperationException(string.Format("Invalid offset value: '{0}' at pattern: '{1}', it should be in format: [d.]hh:mm:ss", offset, pattern));

            return new Pattern { Action = patterAction, DirPath = dirPath, FilePattern = filePattern, Offset = offsetSpan, SubDirs = subDirs.GetValueOrDefault(), DeleteUnCompressed = deleteUnCompressed.GetValueOrDefault(), Size = size.GetValueOrDefault(), DeleteSubDirs = deleteSubDirs.GetValueOrDefault() };
        }
    }
}
