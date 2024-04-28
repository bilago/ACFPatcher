namespace SteamDisableGameUpdateTool.Helpers
{
    public class DirectoryEx
    {
        public static void SetReadOnly(string file)
        {
            if (!File.Exists(file))
                return;
            var finfo = new FileInfo(file);
            if (finfo.IsReadOnly)
                return;

            var attr = File.GetAttributes(file);

            // set read-only
            attr = attr | FileAttributes.ReadOnly;
            File.SetAttributes(file, attr);
        }

        public static string TraverseDirectory(string path, int count)
        {
            var directoryInfo = new DirectoryInfo(path);
            for (int i = 0; i < count && directoryInfo.Parent != null; i++)
            {
                directoryInfo = directoryInfo.Parent;
            }
            return directoryInfo.FullName;
        }

        public static void RemoveReadOnly(string file)
        {
            if (!File.Exists(file))
                return;

            var finfo = new FileInfo(file);
            if (!finfo.IsReadOnly)
                return;

            File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
        }
    }
}
