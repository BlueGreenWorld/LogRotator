using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using log4net;
using CoreSystem.Util;

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
                            .Where(f => f.LastWriteTime < time && f.Length>=this.Size)
                            .ToArray();
            }
            else
                Logger.WarnFormat(string.Format("Directory '{0}' doesn't exists", this.DirInfo.FullName));

            return new FileInfo[] { };
        }

        public void Do()
        {
            Logger.InfoFormat("Processing Pattern {0}", this);
            switch (this.Action)
            {
                case PatternAction.Rotate:
                    foreach (var fileInfo in this.GetFiles())
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
                    break;
                case PatternAction.Delete:
                    foreach (var fileInfo in this.GetFiles())
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

                    break;
                default:
                    throw new NotSupportedException(string.Format("Pattern Action '{0}' not supported/unknown", this.Action));
            }
        }

        public override string ToString()
        {
            return string.Format("[Action: {0}, DirPath: {1}, FilePattern: {2}, Offset: {3:c}, SubDirs: {4}, DeleteUnCompressed: {5}, minSize: {6}]", this.Action, this.DirPath, this.FilePattern, this.Offset, this.SubDirs, this.DeleteUnCompressed,this.Size);
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
            

            PatternAction patterAction;
            if (!Enum.TryParse<PatternAction>(action, true, out patterAction))
                throw new InvalidOperationException(string.Format("Invalid action value: '{0}' at pattern: '{1}', valid values are: ", action, string.Join(", ", Enum.GetNames(typeof(PatternAction)))));

            TimeSpan offsetSpan;
            if (!TimeSpan.TryParse(offset, out offsetSpan))
                throw new InvalidOperationException(string.Format("Invalid offset value: '{0}' at pattern: '{1}', it should be in format: [d.]hh:mm:ss", offset, pattern));

            return new Pattern { Action = patterAction, DirPath = dirPath, FilePattern = filePattern, Offset = offsetSpan, SubDirs = subDirs.GetValueOrDefault(), DeleteUnCompressed = deleteUnCompressed.GetValueOrDefault(), Size=size.GetValueOrDefault() };
        }
    }
}
